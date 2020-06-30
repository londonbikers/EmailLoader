using System.Text;
using System.Xml;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class AttachmentProcessorTestFixture
	{
        // DOCUMENT BUILDER GENERAL ----------------------------------------------------------------------------------

		[Test]
        public void GoogleRichDocumentConversionSuccessTest()
		{
            var builder = new StringBuilder();
            const string wsUrl = "http://docs.google.com/viewer?url=http://el.londonbikers.com/A71EAB060A3FF541B36FD3DD568C9468F46A9A@server1.Jardine.local/2664e6cf-1684-4f7e-8d20-9b9c2eeca8a2.PDF&a=gt";

            using (var reader = new XmlTextReader(wsUrl))
            {
                while (reader.Read())
                {
                    // ignore this element.
                    if (reader.Value.ToLower() == "version=\"1.0\" encoding=\"utf-8\"")
                        continue;

                    builder.Append(reader.Value);
                }
            }

		    var finalText = builder.ToString();
            Assert.IsTrue(!string.IsNullOrEmpty(finalText));
            Assert.IsTrue(finalText.Length > 0);
		}
	}
}