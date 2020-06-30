using System.Collections.Generic;
using System.Configuration;
using MPN.Apollo.EmailLoaderCore;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class LoaderTestFixture
	{
		#region members
		private readonly string _emailHost = ConfigurationManager.AppSettings["EmailHost"];
		private readonly string _emailUsername = ConfigurationManager.AppSettings["EmailUsername"];
		private readonly string _emailPassword = ConfigurationManager.AppSettings["EmailPassword"];
	    #endregion

        // GET IMAP CONNECTION --------------------------------------------------------------------------

		[Test]
        public void GetImapConnectionHostFailureTest()
		{
			const string host = "fail";
            using (var imapConnector = new ImapConnector())
            {
                Assert.Throws<Lesnikowski.Client.ServerException>(() => imapConnector.Connect(host, _emailUsername, _emailPassword));
            }
		}

		[Test]
		public void GetImapConnectionCredentialsFailureTest()
		{
			var host = _emailHost;
			var username = "fail";
			const string password = "fail";

            using (var imapConnector = new ImapConnector())
            {
			    // wrong username & password.
    			var username1 = username;
                Assert.Throws<Lesnikowski.Client.ServerException>(() => imapConnector.Connect(host, username1, password));
            }

            using (var imapConnector = new ImapConnector())
            {
                // wrong password.
                username = _emailUsername;
                Assert.Throws<Lesnikowski.Client.ServerException>(() => imapConnector.Connect(host, username, password));
            }
		}

		[Test]
        public void GetImapConnectionCredentialsSuccessTest()
		{
            using (var imapConnector = new ImapConnector())
            {
                imapConnector.Connect(_emailHost, _emailUsername, _emailPassword);
                Assert.IsTrue(true);
            }
		}

        // GET EMAIL ID's -------------------------------------------------------------------------------

        [Test]
        public void GetEmailIDsSuccessTest()
        {
            using (var imap = new ImapConnector(_emailHost, _emailUsername, _emailPassword))
            {
                Assert.IsNotNull(imap);
                var uids = imap.GetEmailIDs();
                Assert.IsNotNull(uids);
            }
        }

	    // GET EMAIL ------------------------------------------------------------------------------------

		[Test]
		public void GetEmailFailureTest()
		{
            using (var imap = new ImapConnector(_emailHost, _emailUsername, _emailPassword))
            {
                Assert.Throws<Lesnikowski.Client.ServerException>(() => imap.GetEmail(-999L));
            }
		}

		[Test]
		public void GetEmailSuccessTest()
		{
            using (var imap = new ImapConnector(_emailHost, _emailUsername, _emailPassword))
            {
                var uids = imap.GetEmailIDs();
                Assert.IsNotNull(uids);
                Assert.IsTrue(((IList<long>) uids).Count > 0, "Cannot test; no emails found in inbox.");

                var email = imap.GetEmail(((IList<long>) uids)[0]);
                Assert.IsNotNull(email);

                // needs fleshing out to test content.
            }
		}
    }
}
