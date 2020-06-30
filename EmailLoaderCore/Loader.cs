using System;
using System.Threading;
using System.Collections.Specialized;
using MediaPanther.Framework;
using MPN.Apollo.EmailLoaderCore.TetronLoader;

namespace MPN.Apollo.EmailLoaderCore
{
	public class Loader
    {
        #region members
	    private readonly NameValueCollection _config;
	    private bool _run;
        #endregion

        #region constructors
	    /// <summary>
	    /// Creates a new instance of the Email Loader.
	    /// </summary>
	    /// <param name="configuration">The application configuration.</param>
	    /// <exception cref="ArgumentException">If any essential configuration entries are missing from the config, this exception will be thrown.</exception>
	    /// <exception cref="ArgumentNullException">The application configuration is required for the loader to run.</exception>
	    public Loader(NameValueCollection configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (configuration["EmailHost"] == null || configuration["EmailUsername"] == null || configuration["EmailPassword"] == null || configuration["TagDictionaryFilePath"] == null)
                throw new ArgumentException("One or more required configuration values missing from the config file", "config");

            _config = configuration;
        }
        #endregion

        /// <summary>
	    /// Starts the Imap account polling and email processing.
	    /// </summary>
	    public void Start()
        {
            _run = true;
            var pollingInterval = int.Parse(_config["ImapPollingIntervalMs"]);
            var deleteEmails = bool.Parse(_config["EnableEmailDeletion"]);
            var debugMode = bool.Parse(_config["EnableDebugMode"]);
            var deleteFiles = bool.Parse(_config["EnableFilestoreDeletion"]);

            while (_run)
            {
                #region main work
                using (var imapConnector = new ImapConnector(_config["EmailHost"], _config["EmailUsername"], _config["EmailPassword"]))
                {
                    var el = new EditorialLoader();
                    var emailIDs = imapConnector.GetEmailIDs();
                    var docBuilder = new DocumentBuilder(_config);
                    var postImagelessDocuments = bool.Parse(_config["EnablePostingOfImagelessDocuments"]);
                    Document doc = null;

                    foreach (var id in emailIDs)
                    {
                        // we could receive a stop instruction at any time.
                        if (!_run)
                            break;

                        try
                        {
                            var mail = imapConnector.GetEmail(id);
                            doc = docBuilder.BuildDocument(mail);

                            // doc already validated by DocumentBuilder.
                            if (doc == null) continue;

                            // post image-less documents?
                            if (!postImagelessDocuments && doc.Images.Count == 0) continue;

                            // check if this is a duplicate with Apollo.
                            if (el.IsDocumentDuplicateToday(doc.Title)) continue;

                            // send to tetron ws.
                            if (!el.UpdateDocument(doc))
                                Logger.LogError(string.Format("Tetron WS call resulted in a failed response. MessageID: {0}", doc.MessageId));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("Unhandled EL Exception.", ex);
                            if (debugMode) throw;
                        }
                        finally
                        {
                            // delete the message file store - whether or not there was a problem.
                            if (doc != null && deleteFiles) Files.DeleteDirectory(doc.FileStorePath);
                            if (deleteEmails) imapConnector.DeleteEmail(id);
                        }
                    }
                }
                #endregion

                // don't poll the email account for a period to avoid abuse.
                if (_run) Thread.Sleep(pollingInterval);
            }
        }

        /// <summary>
        /// Stops the running of the email loader as soon as the current email has finished processing.
        /// </summary>
        public void Stop()
        {
            _run = false;
        }
	}
}