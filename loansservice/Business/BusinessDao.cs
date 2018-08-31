using System;
using System.Collections.Generic;
using System.Data;
using DBMonoUtility;
using YYLog.ClassLibrary;

public class BusinessDao
{
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
}