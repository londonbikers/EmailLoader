using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Lesnikowski.Mail;
using MediaPanther.Framework.Content;
using MediaPanther.Framework;
using MPN.Apollo.EmailLoaderCore.Models;
using MPN.Apollo.EmailLoaderCore.TetronLoader;

namespace MPN.Apollo.EmailLoaderCore
{
    public class DocumentBuilder
    {
        #region members
        private readonly NameValueCollection _config;
        #endregion

        #region accessors
        public IList<ITag> TagDictionary { get; private set; }
        #endregion

        #region constructors
        public DocumentBuilder(NameValueCollection config)
        {
            _config = config;
            TagDictionary = LoadTagDictionary(_config["tagDictionaryFilePath"]);
        }
        #endregion

        /// <summary>
        /// Attempts to construct a Document from an IMail object.
        /// </summary>
        /// <param name="mail">The source email.</param>
        public Document BuildDocument(IMail mail)
        {
            if (mail == null) throw new ArgumentNullException("mail");
            var doc = new Document
            {
                Title = Text.CapitaliseEachWord(mail.Subject),
                MessageId = mail.MessageID,
                Images = new List<Image>(),
                Tags = new List<string>(),
                Created = DateTime.Now
            };

            BuildBody(doc, mail);
            BuildImages(doc, mail);
            BuildTags(doc);
            BuildAbstract(doc);

            // put this in after the abstract is built so it doesn't end up in the abstract.
            doc.Body = string.Format("<i>Press Release:</i>\n\n{0}", doc.Body);

            // we have the option of saving the built document out to an xml file for debugging/improvement purposes.
            if (bool.Parse(_config["EnableLocalXmlOutput"]))
            {
                var xmlPath = string.Format("{0}xml\\{1}.xml", _config["TemporaryFilePath"], Files.GetSafeFilename(doc.MessageId));
                Xml.SerializeToFile(doc, xmlPath);
            }

            // validate document.
			if (!IsDocumentValid(doc))
			{
				Logger.LogError(string.Format("Invalid Document built! MessageId: {0}", doc.MessageId));
				return null;
			}
			
            return doc;
        }

        /// <summary>
        /// Attempts to generate a set of editorial tags for a document based upon the content.
        /// </summary>
        /// <param name="document">The Document object to attempt to categorise through tags.</param>
        public void BuildTags(Document document)
        {
			foreach (var t in TagDictionary)
			{
                if (Regex.IsMatch(document.Body, string.Format(@"\b{0}\b", t.Name), RegexOptions.IgnoreCase) && !document.Tags.Contains(t.Name))
				{
					document.Tags.Add(t.Name);
					continue;
				}

                if (t.Synonyms.Any(s => Regex.IsMatch(document.Body, string.Format(@"\b{0}\b", s), RegexOptions.IgnoreCase)) && !document.Tags.Contains(t.Name))
			        document.Tags.Add(t.Name);
			}

            if (document.Tags.Count == 0)
                document.Tags.Add("general");
        }

        /// <summary>
        /// Loads the dictionary of tags from an XML reference file.
        /// </summary>
        /// <param name="tagDictionaryFilePath">The full path to the dictionary XML file.</param>
        public IList<ITag> LoadTagDictionary(string tagDictionaryFilePath)
        {
            if (!File.Exists(tagDictionaryFilePath))
                throw new ArgumentException("File does not exist!", "tagDictionaryFilePath");

            var xdoc = XDocument.Load(tagDictionaryFilePath);
            var tags = new List<ITag>();
            foreach (var t in xdoc.Elements("groups").Elements("group").Elements("tag"))
			{
				var tag = new Tag { Name = t.FirstAttribute.Value };
				foreach (var s in t.Elements("synonym"))
					tag.Synonyms.Add(s.Value);
					
				tags.Add(tag);
			}
			
            if (tags.Count == 0)
                Logger.LogError("No tags loaded from dictionary!");

            return tags;
        }

        /// <summary>
        /// Builds the body of the IDocument based upon the best content available within the IMail object.
        /// </summary>
        /// <param name="document">The Document object to build a content body for.</param>
        /// <param name="mail">The source IMail object to build a content body from.</param>
        private static void BuildBody(Document document, IMail mail)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (mail == null) throw new ArgumentNullException("mail");

            if (!string.IsNullOrEmpty(mail.HtmlDataString))
            {
                document.Body = mail.HtmlDataString;
                document.OriginalMessageBody = mail.HtmlDataString;
                document.Body = Text.HtmlToString(document.Body, true);
            }
            else if (!string.IsNullOrEmpty(mail.TextDataString))
            {
                document.Body = mail.TextDataString;
                document.OriginalMessageBody = mail.TextDataString;
                document.Body = Text.HtmlEncodeSpecialChars(Text.RemoveUnwantedNewLines(document.Body));
            }
            else
            {
                Logger.LogDebug(string.Format("No mail body content - Message: {0}", document.MessageId));
            }

            // auto-link content.
            document.Body = Web.UrlsToAnchors(document.Body);
        }

        /// <summary>
        /// Builds the abstract for the IDocument based upon a simplified version of the body.
        /// </summary>
        /// <param name="document">The Document object to build an abstract for.</param>
        private static void BuildAbstract(Document document)
        {
            // remove all the html so we get just text.
            var para = Text.HtmlStripTags(document.Body, true, true);
            var firstPara = Text.GetFirstParagraph(para);

            // we can assume a short first paragraph is an introductory line and not part of the story content
            // so remove it...
            if (firstPara.Length < 55)
                firstPara = Text.GetFirstParagraph(para.Substring(firstPara.Length).Trim());

            document.Abstract = firstPara;
        }

        /// <summary>
        /// Attempts to build a list and collection of processed images for the document.
        /// </summary>
        /// <param name="document">The Document to attach any available images to.</param>
        /// <param name="mail">The IMail object to extract images from.</param>
        private void BuildImages(Document document, IMail mail)
        {
            var ap = new AttachmentProcessor(_config, mail, document);
            ap.ProcessImages();
            ap.ProcessAttachedContent();

            //if (document.Images.Count == 0)
            //    Logger.LogDebug(string.Format("No images found for message: {0}", document.MessageId));
        }
		
		/// <summary>
		/// Ensures an IDocument object is populated according to business rules.
		/// </summary>
		/// <param name="document">The IDocument to validate.</param>
        private static bool IsDocumentValid(Document document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			
			if (string.IsNullOrEmpty(document.Title))
				return false;
				
			if (string.IsNullOrEmpty(document.Abstract))
				return false;
				
			if (string.IsNullOrEmpty(document.Body))
				return false;

            if (document.Tags.Count == 0)
                return false;
				
			if (document.Images != null && document.Images.Count > 0)
			{
				foreach (var image in document.Images)
				{
					if (string.IsNullOrEmpty(image.Name))
						return false;
						
					if (string.IsNullOrEmpty(image.Path))
						return false;
						
					if (image.Width < 1)
						return false;
						
					if (image.Height < 1)
						return false;
				}
			}
		    
            return true;
		}
    }
}