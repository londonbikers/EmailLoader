using System.Collections.Specialized;
using System.Configuration;

namespace EmailLoaderCore.Tests
{
    public static class Helpers
    {
        /// <summary>
        /// Creates a copy of the project configuration so it can be altered by tests.
        /// </summary>
        public static NameValueCollection CopyConfig()
        {
            var nvc = new NameValueCollection();
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                nvc.Add(key, ConfigurationManager.AppSettings[key]);

            return nvc;
        }
    }
}