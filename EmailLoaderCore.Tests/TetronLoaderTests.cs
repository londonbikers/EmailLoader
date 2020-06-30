using System;
using System.Collections.Generic;
using MPN.Apollo.EmailLoaderCore.TetronLoader;
using NUnit.Framework;

namespace EmailLoaderCore.Tests
{
	[TestFixture]
	public class TetronLoaderTestFixture
	{
        // IS DUPLICATE DOCUMENT TESTS --------------------------------------------------------------------------

        /// <summary>
        /// Requires a manual document delete in LB afterwards.
        /// </summary>
		[Test]
        public void IsDocumentDuplicateTodaySuccessTest()
		{
		    var title = "This is document one, test one." + DateTime.Now.Ticks;
            var el = new EditorialLoader();
            var d1 = new Document { Title = title, Abstract = "blah", Body = "blah", OriginalMessageBody = "blah", Tags = new List<string>() };
		    d1.Tags.Add("blah");
		    var d1Result = el.UpdateDocument(d1);
            Assert.IsTrue(d1Result);

            // document doesn't exist.
		    Assert.IsFalse(el.IsDocumentDuplicateToday("This is document two, test one."));
		}

        /// <summary>
        /// Requires a manual document delete in LB afterwards.
        /// </summary>
        [Test]
        public void IsDocumentDuplicateTodayFailureTest()
        {
            var title = "This is document one, test one." + DateTime.Now.Ticks;
            var el = new EditorialLoader();
            var d1 = new Document { Title = title, Abstract = "blah", Body = "blah", OriginalMessageBody = "blah", Tags = new List<string>() };
            d1.Tags.Add("blah");
            var d1Result = el.UpdateDocument(d1);
            Assert.IsTrue(d1Result);

            // document does exist.
            Assert.IsTrue(el.IsDocumentDuplicateToday(title));
        }
    }
}