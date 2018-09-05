using System;
using System.Collections.Generic;
using System.Threading;
using YYLog.ClassLibrary;

public class TaskThread
{
    private Thread _workThread = null;

    private bool _isbreak = false;
    public void Start()
    {
        Log.WriteSystemLog("TaskThread::Start", "The release loan task thread is ready start.");
        _workThread = new Thread(new ThreadStart(threadProc));
        _workThread.Name = "Release Loan Task Thread.";
        _workThread.IsBackground = true;
        _workThread.Start();

        Log.WriteSystemLog("TaskThread::Start", "The thread is already started.");
    }
    private void threadProc()
    {
        while (!_isbreak)
        {
            List<DebitUserRecord> taskList = BusinessDao.GetReadyReleaseDebitRecords();
            LoanBank bank = new LoanBank();
            Log.WriteDebugLog("TaskThread::threadProc", "待放款数据为：{0}条", taskList.Count);
            if (taskList.Count > 0)
            {
                foreach (DebitUserRecord record in taskList)
                {
                    BusinessDao.SetDebitRecordStatus(record.debitId, 5, "Pencairan dana sedang dalam proses");
                    string errMsg = String.Empty;
                    if (bank.Transfer(record, out errMsg))
                    {
                        BusinessDao.SetDebitRecordStatus(record.debitId, 1, "release loan success.");
                    }
                    else
                    {
                        BusinessDao.SetDebitRecordStatus(record.debitId, -1, errMsg);
                    }
                }
            }
            Thread.Sleep(10 * 1000);
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