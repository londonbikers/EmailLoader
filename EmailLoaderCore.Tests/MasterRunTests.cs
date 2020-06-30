using System;
using System.Configuration;
using System.Threading;
using MPN.Apollo.EmailLoaderCore;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class MasterRunTestFixture
	{
		[Test]
        public void RunEmailLoaderTest()
		{
            var loader = new Loader(ConfigurationManager.AppSettings);
            loader.Start();
            Assert.IsTrue(true);
		}

        [Test]
        public void RunEmailLoaderLikeServiceTest()
        {
            var loader = new Loader(ConfigurationManager.AppSettings);
            var st = new ThreadStart(loader.Start);
            var workerThread = new Thread(st);
            workerThread.Start();

            Thread.Sleep(1000 * 60 * 2);

            loader.Stop();
            workerThread.Join(new TimeSpan(0, 0, 2, 0));
            Assert.IsTrue(true);
        }
    }
}