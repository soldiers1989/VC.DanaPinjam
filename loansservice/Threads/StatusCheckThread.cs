using System;
using System.Collections.Generic;
using System.Threading;
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;
using YYLog.ClassLibrary;

public class StatusCheckThread
{
    private Thread _workThread = null;

    private bool _isbreak = false;
    public void Start()
    {
        Log.WriteSystemLog("StatusCheckThread::Start", "The status check task thread is ready start.");
        _workThread = new Thread(new ThreadStart(threadProc));
        _workThread.Name = "Transfer Status Check Task Thread.";
        _workThread.IsBackground = true;
        _workThread.Start();

        Log.WriteSystemLog("StatusCheckThread::Start", "The thread is already started.");
    }
    private void threadProc()
    {
        while (!_isbreak)
        {
            SortedList<string, string> list = BusinessDao.GetNeedCheckDebitRecords();
            Log.WriteDebugLog("StatusCheckThread::threadProc", "存在{0}个需要确认的订单。", list.Count);
            foreach (string orderId in list.Keys)
            {
                string target = list[orderId];
                Log.WriteDebugLog("StatusCheckThread::threadProc", "{0} 准备开始确认订单的状态，渠道{1}。", orderId, target);
                LoanBank bank = new LoanBank();
                string merchantCode = String.Empty;

                InquriyTransferResponse response = bank.DuitkuOrderStatusInquiryRequest(orderId, target);
                if (response.statusCode == "00")
                {
                    CallbackRequestModel callback = new CallbackRequestModel();
                    callback.amount = response.amount;
                    callback.merchantOrderId = response.merchantOrderId;

                    DataProviderResultModel resultModel = DuitkuProvider.SetDuitkuPaybackRecordStaus(callback);
                    if (resultModel.result == Result.SUCCESS)
                    {
                        Log.WriteDebugLog("StatusCheckThread::threadProc", "[{0}]重做成功了。", orderId);
                    }
                    else
                    {
                        Log.WriteDebugLog("StatusCheckThread::threadProc", "[{0}]接口调用成功，但是数据库操作失败了。", orderId);
                    }
                }
                else
                {
                    Log.WriteDebugLog("StatusCheckThread::threadProc", "[{0}]重做失败了。", orderId);
                }

                ///重做次数＋1
                BusinessDao.UpdateRedoUserPayBackRecordStatus(orderId);
            }
            Thread.Sleep(10 * 60 * 1000);
        }
    }

    public void Stop()
    {
        if (null != _workThread)
        {
            _isbreak = true;

            _workThread.Join(2000);

            _workThread.Abort();

            _workThread = null;
        }
    }
}