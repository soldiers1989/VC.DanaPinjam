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
            string sqlStr = @"select debitId,b.BNICode,b.BankCode, b.BankName, b.userId,b.ContactName,a.actualMoney 
                    from IFUserDebitRecord a,IFUserBankInfo b where a.status = @iStatus and a.audit_step = @iStep and 
                    a.userId = b.userId and b.BNICode is not null";
            ParamCollections pc = new ParamCollections();
            pc.Add("@iStatus", 5);
            pc.Add("@iStep", 2);

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
                    float.TryParse(Convert.ToString(dt.Rows[i]["userId"]), out ftmp);
                    record.amountTransfer = ftmp;

                    int.TryParse(Convert.ToString(dt.Rows[i]["debitId"]), out tmp);
                    record.debitId = tmp;

                    record.bankCode = Convert.ToString(dt.Rows[i]["BNICode"]);
                    record.bankAccount = Convert.ToString(dt.Rows[i]["BankCode"]);
                    record.purpose = "duitku auto release loan.Rp" + record.amountTransfer;
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
            string sqlStr = "update IFUserDebitRecord set status = @iStatus,StatusTime = now() where debitId = @iDebitId";
            ParamCollections pc = new ParamCollections();
            pc.Add("@iStatus", status);
            pc.Add("@iDebitId", debitId);
            tran = dbo.BeginTransaction(conn);

            int ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

            sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                        values(@iAuditType, @iDebitId, @iAuditStatus, @sMsg, -1, now());";

            pc.Add("@iAuditType", "5");
            pc.Add("@iDebitId", debitId);
            pc.Add("@iAuditStatus", status);
            pc.Add("@sMsg", auditMsg);

            ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

            tran.Commit();

            return true;
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("BusinessDao::SetDebitRecordStatus", ex.Message);
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