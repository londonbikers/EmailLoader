using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Net;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Lesnikowski.Mail;
using MediaPanther.Framework;
using MediaPanther.Framework.Content;
using MediaPanther.Framework.Imaging;
using MPN.Apollo.EmailLoaderCore.TetronLoader;
using Image = System.Drawing.Image;

namespace MPN.Apollo.EmailLoaderCore
{
    public class AttachmentProcessor
    {
        #region members
        private readonly NameValueCollection _config;
        private readonly IMail _mail;
        private readonly Document _document;
        private readonly int _minPixelSize;
        private readonly int _maxPixelSize;
        private readonly ImageResizer _imageResizer;
        #endregion

        #region constructors
        public AttachmentProcessor(NameValueCollection config, IMail mail, Document document)
        {
            _config = config;
            _document = document;
            _mail = mail;
            _imageResizer = new ImageResizer();

            _minPixelSize = int.Parse(_config["MinimumImagePrimaryDimensionSize"]);
            _maxPixelSize = int.Parse(_config["MaximumImagePrimaryDimensionSize"]);

            // define the location where files will be saved to. this will be exposed by a url as well for ws calls.
            _document.FileStorePath = string.Format("{0}{1}\\", config["TemporaryFilePath"], Files.GetSafeFilename(mail.MessageID));
            CreateMessageFileStore();
        }
        #endregion

        /// <summary>
        /// Attempts to extract and resize if necessary any images attached to the mail or linked in
        /// the mail content itself.
        /// </summary>
        /// <remarks>Multi-threading to make use of multiple-cores via Parallel.ForEach functionality in use.</remarks>
        public void ProcessImages()
        {
            #region attached images
            if (bool.Parse(_config["EnableAttachedImages"]))
            {
                var imageExtensions = new[] {".jpg", ".jpeg", ".png", ".gif", ".bmp"};
                var attachments = _mail.Attachments.Where(item =>
                        (!string.IsNullOrEmpty(item.FileName) && imageExtensions.Contains(Path.GetExtension(item.FileName.ToLower()))) || 
                        item.ContentType.MimeType.ToString().ToLower() == "image");

                Parallel.ForEach(attachments, item =>
                {
                    using (var stream = item.GetMemoryStream())
                        ProcessSlideShowImage(stream, item.FileName);
                });
            }
            #endregion

            #region linked images
            if (!bool.Parse(_config["EnableLinkedImages"])) return;
            // operate on the original message body as anchors/html will have been stripped for the body.
            var filteredLinks = Web.GetLinkUrlsFromHtml(_document.OriginalMessageBody).Where( link =>
                    !string.IsNullOrEmpty(link.Href) && !link.Href.ToLower().StartsWith("mailto:") &&
                    !link.Text.ToLower().Contains("unsubscribe"));

            Parallel.ForEach(filteredLinks, item =>
            {
                WebResponse response = null;
                try
                {
                    #warning todo: some pages come back as html with a single image in, handle this!
                    var req = WebRequest.Create(item.Href);
                    //req.Timeout = 3000; //<-- causes long processing operations to fail.
                    response = req.GetResponse();
                    if (response.ContentType.Contains("image/"))
                    {
                        var stream = response.GetResponseStream();
                        using (var image = Image.FromStream(stream))
                            ProcessSlideShowImage(image, Guid.NewGuid() + ".jpg");
                    }
                }
                catch
                {
                    //Logger.LogDebug(string.Format("Could not process linked content from: {0} - Message: {1}", item.Href, _document.MessageId));
                }
                finally
                {
                    if (response != null) 
                        response.Close();
                }
            });
            #endregion

            if (bool.Parse(_config["EnableCoverImageGeneration"]) && _document.Images.Count > 0)
                GenerateCoverImage();
        }

        /// <summary>
        /// Attempts to extract content from any attached content file, i.e. Microsoft Word or PDF documents
        /// and use the content within them to build the document.
        /// </summary>
        public void ProcessAttachedContent()
        {
            // google docs viewer supported formats...
            var docExtensions = new[] {".pdf", ".doc", ".docx", ".txt"};
            foreach (var mime in _mail.Attachments)
            {
                var isText = false;
                if (mime.ContentType == null || !string.IsNullOrEmpty(mime.FileName) && !docExtensions.Contains(Path.GetExtension(mime.FileName.ToLower())))
                    continue;
                
                if (string.IsNullOrEmpty(mime.FileName) && mime.ContentType.ToString().ToLower() == "text/plain")
                    isText = true;

                // resolve filename.
                var filename = !string.IsNullOrEmpty(mime.FileName)
                                   ? Guid.NewGuid() + Path.GetExtension(mime.FileName)
                                   : Guid.NewGuid().ToString();
                filename = filename.Replace("\\", string.Empty).Replace("/", string.Empty);
                if (isText) filename += ".txt";
                
                #region text-file processing
				// text-files can just be parsed from memory.
                StringBuilder builder;
                if (isText)
				{
					using (var ms = mime.GetMemoryStream())
					using (var reader = new StreamReader(ms))
					{
						builder = new StringBuilder();
						string line;
						while ((line = reader.ReadLine()) != null) 
							builder.Append(line); // <-- may new new-line addition?
							
                        Logger.LogDebug(string.Format("Parsed text file: {0} - Message: {1}", filename, _document.MessageId));

                        // append the text to the document.
					    _document.Body += "\n\n-------[ text-file attachment ]--------\n\n";
						_document.Body += builder.ToString();
						continue;
					}
				}
				#endregion

                #region rich document processing
                #warning no unknown filename support for rich-text documents right now as we'd have to work out a file extension and that's going to take some time.
                if (string.IsNullOrEmpty(mime.FileName)) 
                    continue;

                // make web-service call to the Google Docs Viewer and parse the xml response into a usable string.
                mime.Save(_document.FileStorePath + filename);
                var localDocUrl = string.Format("{0}{1}/{2}", _config["MessageFileStoreBaseUrl"], _mail.MessageID, filename);
                var wsUrl = string.Format(HttpUtility.UrlDecode(_config["GoogleDocsViewerUrl"]), localDocUrl);
                builder = new StringBuilder();
                Logger.LogInfo(string.Format("WS callback Url: {0}", wsUrl));

                #if DEBUG
                wsUrl = string.Format(HttpUtility.UrlDecode(_config["GoogleDocsViewerUrl"]), "http://mediapanther.com/files/invite.pdf");
                #endif

                using (var reader = new XmlTextReader(wsUrl))
                {
                        while (reader.Read())
                        {
                            // ignore this element.
                            if (reader.Value.ToLower() == "version=\"1.0\" encoding=\"utf-8\"")
                                continue;

                            // todo: implement some way of interpreting the node attributes so we can apply some html formatting.
                            builder.Append(reader.Value);
                        }
                }

                Logger.LogDebug(string.Format("Parsed document attachment: {0} - Message: {1} - Content length: {2}", filename, _document.MessageId, builder.Length));

                // append the text to the document.
                _document.Body += "\n\n-------[ pdf/doc attachment ]--------\n\n";
                _document.Body += builder.ToString();
                #endregion

                _document.Body = Text.RemoveUnwantedNewLines(_document.Body);
            }
        }

        #region private methods
        private void CreateMessageFileStore()
        {
            if (!Directory.Exists(_document.FileStorePath))
                Directory.CreateDirectory(_document.FileStorePath);
        }

        /// <summary>
        /// Performs validaiton on images to see if they meet publication requirements.
        /// </summary>
        /// <param name="filename">The raw image filename.</param>
        /// <param name="image">The image to validate.</param>
        private bool IsImageValidForImport(string filename, Image image)
        {
            var isValid = true;

            // ensure the image is of a decent size.
            var size = new Size(image.Width, image.Height);

            if (((size.Width > size.Height || size.Width == size.Height) && size.Width < _minPixelSize) || ((size.Height > size.Width || size.Height == size.Width) && size.Height < _minPixelSize))
            {
                isValid = false;
            }
            // aspect-ratio's that don't look like photos need to be ignored as well.
            else if ((size.Width > size.Height) && (Convert.ToDouble(size.Width) / Convert.ToDouble(size.Height)) > 3D)
            {
                isValid = false;
            }
            else if ((size.Height > size.Width) && (Convert.ToDouble(size.Height) / Convert.ToDouble(size.Width)) > 3D)
            {
                isValid = false;
            }
            // general exclusions based on experience -- move this out to xml if it grows.
            else if (filename.Contains("footer") || filename.Contains("header"))
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Validates an image for import, resizes it if necessary and saves it to the message store, then adds a reference to the Document object.
        /// </summary>
        /// <param name="imageStream">The raw image byte stream to read the image from.</param>
        /// <param name="filename">The original filename for the image.</param>
        private void ProcessSlideShowImage(Stream imageStream, string filename)
		{
            ProcessSlideShowImage(Image.FromStream(imageStream), filename);
		}

        /// <summary>
        /// Validates an image for import, resizes it if necessary and saves it to the message store, then adds a reference to the Document object.
        /// </summary>
        /// <param name="image">The image itself to process.</param>
        /// <param name="filename">The original filename for the image.</param>
        private void ProcessSlideShowImage(Image image, string filename)
        {
            if (!IsImageValidForImport(filename, image))
                return;
            // -- put in meta-data parsing code here to pull out image data.
            filename = filename.Replace("\\", string.Empty).Replace("/", string.Empty);
            var path = _document.FileStorePath + filename;

            // save a copy of the original image as well for possible processing later.
            var originalFilename = Files.AppendToFilename(path, "-original", true);
            image.Save(originalFilename);

            using (var outputImage = _imageResizer.ResizeImage(image, _maxPixelSize))
            {
                _imageResizer.SaveImageFile(outputImage, path);

                // add Image to Document -- need to lock as this is running in a multi-threaded context.
                var docImage = new TetronLoader.Image { Path = path, Width = outputImage.Width, Height = outputImage.Height, Name = filename, Created = DateTime.Now, IsSlideshowImage = true };
                lock (_document.Images)
                    _document.Images.Add(docImage);
            }
        }

        /// <summary>
        /// Generates a cover image from one of the existing document images.
        /// </summary>
        private void GenerateCoverImage()
        {
            if (_document.Images.Count == 0)
                throw new Exception("Document has no images!");

            // -- find a suitable image.
            // 1- ideally a portrait photo.
            // 2- failing that, the one that crops best to portrait aspect, i.e. is most square.

            var sourceImage = _document.Images.FirstOrDefault(q => q.Height > q.Width) ?? 
                              _document.Images.First(q=>q.Width / q.Height == _document.Images.Min(q2 => q2.Width / q2.Height));

            CreateCoverImageFromExisting(sourceImage);
        }

        /// <summary>
        /// Creates a new cover image from an existing one.
        /// </summary>
        private void CreateCoverImageFromExisting(TetronLoader.Image image)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            var namePart = Path.GetFileNameWithoutExtension(image.Path);
            var filename = image.Path.Replace(namePart, namePart + "-cover");
            using (var sourceImageFile = Image.FromFile(Files.AppendToFilename(image.Path, "-original", true)))
            using (var outputImage = _imageResizer.ResizeImage(sourceImageFile, 200, ImageResizer.Axis.Horizontal))
            {
                _imageResizer.SaveImageFile(outputImage, filename);
                var editorialImage = new TetronLoader.Image
                {
                    IsCoverImage = true,
                    Width = outputImage.Width,
                    Height = outputImage.Height,
                    Path = filename,
                    Name = image.Name
                };
                _document.Images.Insert(0, editorialImage);
            }
        }
        #endregion
    }
}