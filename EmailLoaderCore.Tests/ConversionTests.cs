using System.IO;
using MediaPanther.Framework;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class ConversionTestFixture
	{
        // HTML->TEXT CONVERSION TESTS ----------------------------------------------------------------------------------

        /// <summary>
        /// Test requires a debug to human-verify the output.
        /// </summary>
		[Test]
        public void MsWordConversionSuccessTest()
		{
            var html = File.ReadAllText(@"C:\Source\EmailLoaderCore\EmailLoaderCore.Tests\Documents\word.html");
            Assert.IsFalse(string.IsNullOrEmpty(html));
            var text = MediaPanther.Framework.Content.Text.HtmlToString(html);
            Assert.IsFalse(string.IsNullOrEmpty(text));
		}

        /// <summary>
        /// Test requires a debug to human-verify the output.
        /// </summary>
        [Test]
        public void AceCafeConversionSuccessTest()
        {
            var html = File.ReadAllText(@"C:\Source\EmailLoaderCore\EmailLoaderCore.Tests\Documents\ace-cafe.html");
            Assert.IsFalse(string.IsNullOrEmpty(html));
            var text = MediaPanther.Framework.Content.Text.HtmlToString(html);
            Assert.IsFalse(string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Test requires a debug to human-verify the output.
        /// </summary>
        [Test]
        public void FabBikerConversionSuccessTest()
        {
            var html = File.ReadAllText(@"C:\Source\EmailLoaderCore\EmailLoaderCore.Tests\Documents\fab-biker.html");
            Assert.IsFalse(string.IsNullOrEmpty(html));
            var text = MediaPanther.Framework.Content.Text.HtmlToString(html, true);
            Assert.IsFalse(string.IsNullOrEmpty(text));
            var linkedText = Web.UrlsToAnchors(text);
            Assert.IsFalse(string.IsNullOrEmpty(linkedText));
        }
	}
}