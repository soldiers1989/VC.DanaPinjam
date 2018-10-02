using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using stockmoniter.Dao;
using YYLog.ClassLibrary;

/// <summary>
/// Buy record moniter.
/// </summary>
public class PinjamSMSSendter
{
    /// <summary>
    /// The file moniter.
    /// </summary>
    private Thread _fileMoniter = null;

    /// <summary>
    /// The is break.
    /// </summary>
    private bool _isBreak = false;

    /// <summary>
    /// The hashtable.
    /// </summary>
    private static Hashtable _hashtable = new Hashtable();

    public void Start()
    {
        if (null == _fileMoniter)
        {
            _fileMoniter = new Thread(new ParameterizedThreadStart(monitor));
            _fileMoniter.Name = "pinjam-sms-send-moniter-thread";
            _fileMoniter.IsBackground = true;
            _fileMoniter.Start();
        }
    }

    void monitor(object state)
    {
        while (!_isBreak)
        {

            if (DateTime.Now.Hour > 9)
            {
                List<DebitRecord> list = SMSSendDao.GetDebitRecords();

                Console.WriteLine("总共有：" + list.Count + "需要检查是否发送短信。");
                WaveCellSMSSingleSender sms = new WaveCellSMSSingleSender();
                WaveCellSMSSingleSender.Authorization = "Bearer yCCTxuCM7nIbdEuIxENllGMuqlF90qjtMlhb201S0bI";
                WaveCellSMSSingleSender.SubAccountName = "Prodigy_DANA";
                int day2 = 0, day1 = 0, day0 = 0;

                foreach (DebitRecord debitRecord in list)
                {
                    WaveCellSMSResponseModels result = new WaveCellSMSResponseModels();
                    
                    bool hasSend = false;
                    switch (debitRecord.overdueDay)
                    {
                        case 2:
                            if (debitRecord.smsSendTimes == 0)
                            {
                                hasSend = true;
                                result = sms.Send("+62"+debitRecord.phone, @"Pinjaman anda akan jatuh 2 hari lagi, Jumlah: Rp.2 000.000. Info cara pengembalian dan perpanjangan ada di aplikasi. Terima kasih");
                            }
                            break;
                        case 1:
                            if (debitRecord.smsSendTimes <= 1)
                            {
                                hasSend = true;
                                result = sms.Send("+62"+debitRecord.phone, @"1 hari lagi akan jatuh Tempo Pinjaman anda. Jumlah:Rp.2 000.000.Info cara pengembalian dan perpanjangan ada diaplikasi.Terima kasih");
                            }
                            break;
                        case 0:
                            if (debitRecord.smsSendTimes <= 2)
                            {
                                hasSend = true;
                                result = sms.Send("+62" + debitRecord.phone, @"TRANSFER KE BANK MANDIRI 168 000 1281 722 PT. ANUGERAH DIGITAL NIAGA untuk menghindari data anda diproses OJK & COLLECTOR.");
                            }
                            break;
                        default:
                            break;
                    }

                    if (result.status.code == "QUEUED")
                    {
                        if (debitRecord.overdueDay == 2)
                        {
                            day2++;
                            SMSSendDao.UpdateDebitSMSStatus(debitRecord);
                            //Log.WriteSystemLog("PinjamSMSSendter::moniter", "发送成功：{0} , {1}", debitRecord.phone, debitRecord.overdueDay
                        }

                        if (debitRecord.overdueDay == 1)
                        {
                            day1++;
                            SMSSendDao.UpdateDebitSMSStatus(debitRecord);
                            //Log.WriteSystemLog("PinjamSMSSendter::moniter", "发送成功：{0} , {1}", debitRecord.phone, debitRecord.overdueDay);

                        }

                        if (debitRecord.overdueDay == 0)
                        {
                            day0++;

                            SMSSendDao.UpdateDebitSMSStatus(debitRecord);
                            //Log.WriteSystemLog("PinjamSMSSendter::moniter", "发送成功：{0} , {1}", debitRecord.phone, debitRecord.overdueDay);

                        }
                    }
                    else
                    {
                        if (hasSend)
                            Log.WriteErrorLog("PinjamSMSSendter::moniter", "发送失败：{0} , {1}", debitRecord.phone, result.status.description);
                    }

                }
                Log.WriteSystemLog("PinjamSMSSendter::moniter", "day2 = {0}| day1 = {1} | day0 = {2}", day2, day1, day0);
            }
            else
            {
                Log.WriteSystemLog("PinjamSMSSendter::moniter", "未到发送时：09-15点");
            }
            int sleepTime = 10;

            ///10 分钟监控一次数据
            Thread.Sleep(1000 * 60 * sleepTime);
        }
    }

    public void Stop()
    {
        if (null != _fileMoniter)
        {
            _fileMoniter.Join();

            _fileMoniter.Abort();

            _fileMoniter = null;
        }

    }
}