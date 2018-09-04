using System;
using DBMonoUtility;
using YYLog.ClassLibrary;

namespace loansservice
{
    public class ControlCenter
    {
        private TaskThread taskThread = null;
        public void Start()
        {
            Log.Init(1, 50240000, "yyyyMMdd", @"./logs/", LogType.Debug);

            DataBaseOperator.SetDbIniFilePath(".");
            Log.WriteDebugLog("ControlCenter::Startup", "Begin connect db");
            
            string connStr = DataBasePool.AddDataBaseConnectionString("debit", "!%(**$*@^77f1fjj", 5, 5);

            Log.WriteDebugLog("ControlCenter::Startup", connStr);
            DataBaseOperator.Init("debit");
            taskThread = new TaskThread();
            taskThread.Start();
        }

        public void Stop()
        {
            taskThread.Stop();
        }
    }
}
