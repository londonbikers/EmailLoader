using System;
using System.Linq;
using System.Diagnostics;
using System.Configuration;

namespace MPN.Apollo.EmailLoaderCore
{
    public static class Logger
    {
        #region members
        private static readonly string Source;
        private static readonly string[] LoggingLevels;
        #endregion

        #region constructors
        static Logger()
        {
            Source = ConfigurationManager.AppSettings["ApplicationName"];
            LoggingLevels = ConfigurationManager.AppSettings["LoggingLevels"].ToLower().Split(char.Parse(","));
        }
        #endregion

        #region public methods
        public static void LogDebug(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException("message");
            if (!LoggingLevels.Contains("debug")) return;

            InitialiseEventLog();
            EventLog.WriteEntry(Source, message, EventLogEntryType.Information);
        }

        public static void LogInfo(string message)
        {
            LogDebug(message);
        }

        public static void LogWarning(string warning)
        {
            if (string.IsNullOrEmpty(warning)) throw new ArgumentNullException("warning");
            if (!LoggingLevels.Contains("warning")) return;
            InitialiseEventLog();
            EventLog.WriteEntry(Source, warning, EventLogEntryType.Warning);
        }

        public static void LogWarning(string warning, Exception exception)
        {
            if (string.IsNullOrEmpty("warning")) throw new ArgumentNullException("warning");
            if (exception == null) throw new ArgumentNullException("exception");

            if (!LoggingLevels.Contains("warning")) return;
            InitialiseEventLog();
            var message = warning + "\n\n" + exception.Message + "\n\n" + exception.StackTrace;
            EventLog.WriteEntry(Source, message, EventLogEntryType.Warning);
        }

        public static void LogError(string error, Exception exception)
        {
            if (string.IsNullOrEmpty("error")) throw new ArgumentNullException("error");
            if (exception == null) throw new ArgumentNullException("exception");

            if (!LoggingLevels.Contains("error")) return;
            InitialiseEventLog();
            var message = error + "\n\n" + exception.Message + "\n\n" + exception.StackTrace;
            EventLog.WriteEntry(Source, message, EventLogEntryType.Error);
        }

        public static void LogError(string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            if (!LoggingLevels.Contains("error")) return;
            InitialiseEventLog();
            EventLog.WriteEntry(Source, message, EventLogEntryType.Error);
        }
        #endregion

        #region private methods
        private static void InitialiseEventLog()
        {
            if (!EventLog.SourceExists("Apollo Email Loader"))
                EventLog.CreateEventSource(Source, Source);
        }
        #endregion
    }
}