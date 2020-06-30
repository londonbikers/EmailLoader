using System;
using System.Configuration;
using MPN.Apollo.EmailLoaderCore;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class DocumentBuilderTestFixture
	{
        // DOCUMENT BUILDER GENERAL ----------------------------------------------------------------------------------

		[Test]
        public void CreateDocumentBuilderSuccessTest()
		{
		    var docBuilder = new DocumentBuilder(ConfigurationManager.AppSettings);
            Assert.IsNotNull(docBuilder, "Expected the DocumentBuilder instance not to be null.");
            Assert.IsNotNull(docBuilder.TagDictionary, "Expected the tag dictionary to not be null.");
            Assert.IsTrue(docBuilder.TagDictionary.Count > 0, "Expected some tags to be present in the dictionary");
		}

        [Test]
        public void CreateDocumentBuilderFailureTest()
        {
            var newConfig = Helpers.CopyConfig();
            newConfig["TagDictionaryFilePath"] = "incorrect file path";
            Assert.Throws<ArgumentException>(() => new DocumentBuilder(newConfig));
        }
	}
}