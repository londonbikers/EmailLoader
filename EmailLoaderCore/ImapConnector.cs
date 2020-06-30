using System;
using System.Collections.Generic;
using Lesnikowski.Client.IMAP;
using Lesnikowski.Mail;

namespace MPN.Apollo.EmailLoaderCore
{
    public class ImapConnector : IDisposable
    {
        #region members
        private Imap _imap;
        #endregion

        #region constructors
        public ImapConnector()
        {
        }

        public ImapConnector(string host, string username, string password)
        {
            Connect(host, username, password);
        }
        #endregion

        /// <summary>
        /// Establishes a connection to the email host via IMAP. Be sure to Close() when done.
        /// </summary>
        /// <param name="host">The hostname for the IMAP server to connect to.</param>
        /// <param name="username">The IMAP account username to connect with.</param>
        /// <param name="password">The IMAP account password to connect with.</param>
        public void Connect(string host, string username, string password)
        {
            if (_imap != null && _imap.Connected)
                _imap.Close();

            _imap = new Imap();
            _imap.ConnectSSL(host);
            _imap.Login(username, password);
            _imap.SelectInbox();
        }

        /// <summary>
        /// Returns a list of the ID's for emails in the news reader account.
        /// </summary>
        /// <returns>A list of numeric ID's for individual emails in the inbox of the account.</returns>
        public IEnumerable<long> GetEmailIDs()
        {
            return _imap.SearchFlag(Flag.Unseen);
        }

        /// <summary>
        ///  Retrieves a specific email by it's unique-identifier.
        /// </summary>
        /// <param name="uid">The unique-identifier for the email with the host.</param>
        public IMail GetEmail(long uid)
        {
            var eml = _imap.GetMessageByUID(uid);
            return new MailBuilder().CreateFromEml(eml);
        }

        /// <summary>
        /// Permenantly deletes a specific email from the host.
        /// </summary>
        /// <param name="uid">The unique-identifier for the email with the host.</param>
        public void DeleteEmail(long uid)
        {
            _imap.DeleteMessageByUID(uid);
        }

        #region IDisposable Members
        public void Dispose()
        {
            if (_imap != null)
                _imap.Dispose();
        }
        #endregion
    }
}