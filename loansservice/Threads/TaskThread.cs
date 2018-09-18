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
            int week = Convert.ToInt32(DateTime.Now.DayOfWeek.ToString("d"));
            int beginHour = 8;
            int endHour = 21;
            if (week > 0 && week < 6)
            {
                //if (DateTime.Now.Hour >= beginHour && DateTime.Now.Hour <= endHour)
                if (true)
                {
                    List<DebitUserRecord> taskList = BusinessDao.GetReadyReleaseDebitRecords();
                    LoanBank bank = new LoanBank();
                    Log.WriteDebugLog("TaskThread::threadProc", "今天是星期:{0} 是放款日,放款时间段：{1}点- {2}点，待放款数据为：{3}条"
                                                    , week, beginHour, endHour, taskList.Count);

                    if (taskList.Count > 0)
                    {
                        foreach (DebitUserRecord record in taskList)
                        {
                            try
                            {
                                BusinessDao.SetDebitRecordStatus(record.debitId, 5, "Pencairan dana sedang dalam proses(10002)");
                                string errMsg = String.Empty;
                                if (bank.Transfer(record, out errMsg))
                                {
                                    BusinessDao.SetDebitRecordStatus(record.debitId, 1, "Uang anda telah berhasil di transfer(10002).");
                                }
                                else
                                {
                                    BusinessDao.SetDebitRecordStatus(record.debitId, -1, errMsg);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteErrorLog("TaskThread::threadProc", ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    Log.WriteDebugLog("TaskThread::threadProc", "今天是星期:{0} 放款时间段：{1}点- {2}点，现在是：{3}点"
                                                    , week, beginHour, endHour, DateTime.Now.Hour);
                }
            }
            else
            {
                Log.WriteDebugLog("TaskThread::threadProc", "今天是星期:{0} 不放款。", week);
            }
            Thread.Sleep(60 * 1000);
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