using System;
using System.Threading;
using DBMonoUtility;
using loansservice;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using YYLog.ClassLibrary;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            //AGUSTINUS WAHYU TETRA NO      !=agustinus wahyu tetra novranta
            //：             !=

            Console.WriteLine(Convert.ToString(Levenshtein("CHINTIA D W LASUT", "chintia dervini wulandari lasut".ToUpper())));
             
            Log.Init(1, 50240000, "yyyyMMdd", @"./logs/", LogType.Debug);

            DataBaseOperator.SetDbIniFilePath(".");
            Log.WriteDebugLog("ControlCenter::Startup", "Begin connect db");
            
            string connStr = DataBasePool.AddDataBaseConnectionString("debittest", "!%(**$*@^77f1fjj", 5, 5);

            Log.WriteDebugLog("ControlCenter::Startup", connStr);
            DataBaseOperator.Init("debittest");

            ControlCenter cc = new ControlCenter();

            cc.Start();

            while(true)
            {
                Thread.Sleep(100000);
            }
              
            return;
            string serverInfo = "127.0.0.1:6379";
            string password = "123!@#qweASD";
            RedisPools.RedisPools.Init(serverInfo, Proxy.None, 200, password);

            LoanBank bank = new LoanBank();

            DebitUserRecord record = new DebitUserRecord();
            record.debitId = 111;
            record.bankAccount = "1680001297876";
            record.bankCode = "008";
            record.amountTransfer = 10000;
            record.purpose = "test";
            record.userId = 27;
            record.userName = "HENDRA";

            string errMsg = String.Empty;

            bank.Transfer(record, out errMsg);


            //bank.CheckTransferStatus("10013");
            //$paramSignature = $email . $timestamp . $bankCode . $bankAccount . $accountName . $custRefNumber . $amountTransfer . $purpose . $disburseId . $secretKey; 
        }

        /// <summary>
        /// 比较两个字符串的相似度，并返回相似率。
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static float Levenshtein(string str1, string str2)
        {
            char[] char1 = str1.ToCharArray();
            char[] char2 = str2.ToCharArray();
            //计算两个字符串的长度。  
            int len1 = char1.Length;
            int len2 = char2.Length;
            //建二维数组，比字符长度大一个空间  
            int[,] dif = new int[len1 + 1, len2 + 1];
            //赋初值  
            for (int a = 0; a <= len1; a++)
            {
                dif[a, 0] = a;
            }
            for (int a = 0; a <= len2; a++)
            {
                dif[0, a] = a;
            }
            //计算两个字符是否一样，计算左上的值  
            int temp;
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    if (char1[i - 1] == char2[j - 1])
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = 1;
                    }
                    //取三个值中最小的  
                    dif[i, j] = Min(dif[i - 1, j - 1] + temp, dif[i, j - 1] + 1, dif[i - 1, j] + 1);
                }
            }
            //计算相似度  
            float similarity = 1 - (float)dif[len1, len2] / Math.Max(len1, len2);
            return similarity;
        }

        /// <summary>
        /// 求最小值
        /// </summary>
        /// <param name="nums"></param>
        /// <returns></returns>
        private static int Min(params int[] nums)
        {
            int min = int.MaxValue;
            foreach (int item in nums)
            {
                if (min > item)
                {
                    min = item;
                }
            }
            return min;
        }
    }
}
