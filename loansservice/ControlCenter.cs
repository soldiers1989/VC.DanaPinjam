using System;
using DBMonoUtility;
using Microsoft.Extensions.Configuration;
using YYLog.ClassLibrary;

namespace loansservice
{
    public class ControlCenter
    {
        private TaskThread taskThread = null;

        private StatusCheckThread statusThread = null;
        public void Start()
        {
            taskThread = new TaskThread();
            taskThread.Start();

            statusThread = new StatusCheckThread();
            statusThread.Start();
        }

        public void Stop()
        {
            taskThread.Stop();
            statusThread.Stop();
        }
    }
}
