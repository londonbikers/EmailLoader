using System;
using MPN.Apollo.EmailLoaderCore;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class LoggingTestFixture
	{

		[Test]
        public void BasicLoggingSuccessTest()
		{
            var ex = new Exception("Something went wrong");
		    Logger.LogDebug("Some error text.");
            Logger.LogError("Some error text.");
            Logger.LogError("Some error text.", ex);
            Logger.LogInfo("Some error text.");
            Logger.LogWarning("Some error text.");
            Logger.LogWarning("Some error text.", ex);
            Assert.IsTrue(true);
		}

        [Test]
        public void BasicLoggingFailureTest()
        {
            var ex = new Exception("Something went wrong");
            try
            {
                Logger.LogDebug(string.Empty);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogError(string.Empty);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogError(string.Empty, ex);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogError("Something went wrong.", null);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogInfo(string.Empty);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogWarning(string.Empty);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogWarning(string.Empty, ex);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }

            try
            {
                Logger.LogWarning("Something went wrong.", null);
                Assert.IsTrue(false, "Should have thrown an exception!");
            }
            catch
            {
                Assert.IsTrue(true);
            }
        }
	}
}