using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Configuration;
using System.Threading;

namespace MPN.Apollo.EmailLoaderService
{
    public partial class ElService : ServiceBase
    {
        #region members
        private readonly EmailLoaderCore.Loader _loader;
        private Thread _workerThread;
        #endregion

        public ElService()
        {
            InitializeComponent();
            _loader = new EmailLoaderCore.Loader(ConfigurationManager.AppSettings);
        }

        protected override void OnStart(string[] args)
        {
            var st = new ThreadStart(_loader.Start);
            _workerThread = new Thread(st);
            _workerThread.Start();

            EventLog.WriteEntry("Apollo Email Loader started.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _loader.Stop();
            _workerThread.Join(new TimeSpan(0, 0, 2, 0));
            EventLog.WriteEntry("Apollo Email Loader stopped.", EventLogEntryType.Information);
        }
    }
}