using System;
using System.Collections.Generic;
using System.Data;
using DBMonoUtility;
using YYLog.ClassLibrary;

public class BusinessDao
{
    ///
    /// 获取待自动放款的记录
    ///
    public static List<DebitUserRecord> GetReadyReleaseDebitRecords()
    {
        DataBaseOperator dbo = null;
        List<DebitUserRecord> list = new List<DebitUserRecord>();
        try
        {
            dbo = new DataBaseOperator();
            string sqlStr = @"select debitId,b.BNICode,b.BankCode, b.BankName, b.userId,b.ContactName,a.actualMoney,ifnull(a.target,'A') target
                    from IFUserDebitRecord a,IFUserBankInfo b where a.status = @iStatus and a.audit_step = @iStep and 
                    a.bankId = b.bankId and b.BNICode is not null and b.BNICode != '' limit 10";
            ParamCollections pc = new ParamCollections();
            pc.Add("@iStatus", 5);
            pc.Add("@iStep", 3);

            DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());
            if (null != dt && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DebitUserRecord record = new DebitUserRecord();
                    int tmp = 0;
                    int.TryParse(Convert.ToString(dt.Rows[i]["userId"]), out tmp);
                    record.userId = tmp;

                    record.userName = Convert.ToString(dt.Rows[i]["contactName"]);
                    float ftmp = 0f;
                    float.TryParse(Convert.ToString(dt.Rows[i]["actualMoney"]), out ftmp);
                    record.amountTransfer = ftmp;

                    int.TryParse(Convert.ToString(dt.Rows[i]["debitId"]), out tmp);
                    record.debitId = tmp;

                    record.bankCode = Convert.ToString(dt.Rows[i]["BNICode"]);
                    record.target = Convert.ToString(dt.Rows[i]["target"]);
                    record.bankAccount = Convert.ToString(dt.Rows[i]["BankCode"]);
                    record.purpose = "auto release loan.Rp" + record.amountTransfer;
                    list.Add(record);
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("BusinessDao::GetReadyReleaseDebitRecords", ex.Message);
        }
        finally
        {
            if (null != dbo)
            {
                dbo.Close();
                dbo = null;
            }
        }
        return list;
    }

    public static SortedList<string, string> GetNeedCheckDebitRecords()
    {
        DataBaseOperator dbo = null;
        SortedList<string, string> list = new SortedList<string, string>();
        try
        {
            dbo = new DataBaseOperator();
            string sqlStr = @"select id,ifnull(b.target,'A') target from IFUserPayBackDebitRecord a,IFUserDebitRecord b
                where a.type in (3,4) and a.status = -2 and ifnull(a.reTryTimes,0) < 3 
				and a.debitId = b.debitId
                and a.createTime < date_add(now(), interval -30 minute) 
                and a.createTime > '2018-09-13 22:00:00' order by a.reTryTimes limit 10";
            ParamCollections pc = new ParamCollections();

            DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());
            if (null != dt && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    list.Add(Convert.ToString(dt.Rows[i]["id"]), Convert.ToString(dt.Rows[i]["target"]));
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("BusinessDao::GetNeedCheckDebitRecords", ex.Message);
        }
        finally
        {
            if (null != dbo)
            {
                dbo.Close();
                dbo = null;
            }
        }
        return list;
    }

    public static bool UpdateRedoUserPayBackRecordStatus(string orderId)
    {
        DataBaseOperator dbo = null;
        try
        {
            dbo = new DataBaseOperator();
            string sqlStr = @"update IFUserPayBackDebitRecord set statusTime=now(),retryTimes=ifnull(retryTimes,0)+1 where id=@iOrderId";
            ParamCollections pc = new ParamCollections();
            pc.Add("@iOrderId", orderId);
            dbo.ExecuteStatement(sqlStr, pc.GetParams());
            return true;
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("BusinessDao::UpdateUserPayBackRecordStatus", ex.Message);
        }
        finally
        {
            if (null != dbo)
            {
                dbo.Close();
                dbo = null;
            }
        }
        return false;
    }
    ///设置贷款记录的状态
    public static bool SetDebitRecordStatus(int debitId, int status, string auditMsg)
    {
        DataBaseOperator dbo = null;
        IDbTransaction tran = null;
        IDbConnection conn = null;
        try
        {
            dbo = new DataBaseOperator();
            conn = dbo.GetConnection();

            string sqlStr = String.Empty;
            ParamCollections pc = new ParamCollections();
            if (status == 1)
            {
                sqlStr = "update IFUserDebitRecord set status = @iStatus,StatusTime = now(),releaseLoanTime=now(),payBackDayTime=date_add(now(),interval 7 day) where debitId = @iDebitId";
                pc.Add("@iStatus", status);
                pc.Add("@iDebitId", debitId);
                Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "放款成功，{0} － {1}", debitId, status);
            }
            else
            {
                sqlStr = "update IFUserDebitRecord set status = @iStatus,StatusTime = now() where debitId = @iDebitId";
                pc.Add("@iStatus", status);
                pc.Add("@iDebitId", debitId);
                Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "设置状态，{0} - {1}", debitId, status);
            }

            tran = dbo.BeginTransaction(conn);

            int ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
            Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "执行成功({0}){1} － {2}", ret, debitId, status);
            Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "准备插入描述 {0} - {1} {2}", debitId, status, auditMsg);

            sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                        values(@iAuditType, @iDebitId, @iAuditStatus, @sMsg, -1, now());";

            pc.Add("@iAuditType", "5");
            pc.Add("@iDebitId", debitId);
            pc.Add("@iAuditStatus", status);
            pc.Add("@sMsg", auditMsg);

            ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
            Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "执行成功({0}){1} － {2}", ret, debitId, status);

            if (status == 1)
            {
                string releaseLogSql = @"insert into IFUserDebitPaybackTimeChange(debitId, changeDays, afterTime,
                                                                                                     changeType, remarks,createTime,adminId,objectId)
                                                                                    values(@iDebitId, @iChangeDays, date_add(now(),interval 7 day), 
                                                                                                    @iChangeType, @sRemarks, now(), @iAdminId, @iObjectId);";
                pc.Add("@iDebitId", debitId);
                pc.Add("@iChangeDays", 7);
                pc.Add("@iChangeType", 3);
                pc.Add("@sRemarks", auditMsg);
                pc.Add("@iAdminId", "-1");
                pc.Add("@iObjectId", debitId);

                int dbret = dbo.ExecuteStatement(releaseLogSql, pc.GetParams(true), conn);
                Log.WriteDebugLog("BusinesssDao::SetDebitRecordStatus", "[{0}]插入贷款的还款时间变更记录 成功({1})。", debitId, dbret);
            }
            tran.Commit();

            return true;
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("BusinessDao::SetDebitRecordStatus", "出现异常{0}", ex.Message);
        }
        finally
        {
            if (null != dbo)
            {
                if (null != conn)
                {
                    dbo.ReleaseConnection(conn);
                }
                dbo.Close();
                dbo = null;
            }
        }

        if (null != tran)
        {
            tran.Rollback();
            tran = null;
        }
        return false;
    }
}