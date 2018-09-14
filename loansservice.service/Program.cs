using System;
using System.Threading;
using DBMonoUtility;
using loansservice;
using Newtonsoft.Json;
using YYLog.ClassLibrary;

namespace loansservice.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            Log.Init(1, 50240000, "yyyyMMdd", @"./logs/", LogType.Debug);

            DataBaseOperator.SetDbIniFilePath(".");
            Log.WriteDebugLog("ControlCenter::Startup", "Begin connect db");
            
            string connStr = DataBasePool.AddDataBaseConnectionString("debit", "!%(**$*@^77f1fjj", 5, 5);

            Log.WriteDebugLog("ControlCenter::Startup", connStr);
            DataBaseOperator.Init("debit");

            ControlCenter cc = new ControlCenter();

            cc.Start();

            while(true)
            {
                Thread.Sleep(100000);
            }
        }
    }
}
