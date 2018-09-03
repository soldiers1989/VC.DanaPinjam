using System;
using DBMonoUtility;

namespace loansservice
{
    public class ControlCenter
    {
        private TaskThread taskThread = null;
        public void Start()
        {
            DataBasePool.AddDataBaseConnectionString("debit", "!%(**$*@^77f1fjj", 5, 5);
            taskThread = new TaskThread();
            taskThread.Start();
        }

        public void Stop()
        {
            taskThread.Stop();
        }
    }
}
