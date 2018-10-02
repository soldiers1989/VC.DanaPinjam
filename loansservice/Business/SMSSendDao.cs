using System;
using System.Collections.Generic;
using System.Data;
using DBMonoUtility;
using YYLog.ClassLibrary;

namespace stockmoniter.Dao
{
    public class SMSSendDao
    {
        public static List<DebitRecord> GetDebitRecords()
        {
            DataBaseOperator dataBaseOperator = null;
            List<DebitRecord> list = new List<DebitRecord>();
            try
            {
                dataBaseOperator = new DataBaseOperator();
                ParamCollections paramCollections = new ParamCollections();

                string sqlStr = @"select * from (
select debitId,b.Phone,datediff(date_format(payBackDayTime, '%Y-%m-%d'),date_format(now(), '%Y-%m-%d')) overdueDay,ifnull(smsSendTimes,0) smsSendTimes 
from IFUserDebitRecord a,IFUsers b where a.userId = b.userId and a.status =@iStatus ) as tab where overdueDay < @iOverdueDay;";
                paramCollections.Add("@iStatus", 1);
                paramCollections.Add("@iOverdueDay", 3);
                DataTable dt = dataBaseOperator.GetTable(sqlStr, paramCollections.GetParams());

                if (null != dt)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DebitRecord debit = new DebitRecord();
                        int itmp = 0;
                        int.TryParse(Convert.ToString(dt.Rows[i]["debitId"]), out itmp);
                        debit.debitId = itmp;

                        int.TryParse(Convert.ToString(dt.Rows[i]["overdueDay"]), out itmp);
                        debit.overdueDay = itmp;

                        int.TryParse(Convert.ToString(dt.Rows[i]["smsSendTimes"]), out itmp);
                        debit.smsSendTimes = itmp;

                        debit.phone = Convert.ToString(dt.Rows[i]["phone"]);
                        list.Add(debit);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("SMSSendDao::GetDebitRecords", ex.Message);
            }
            return list;
        }

        public static bool UpdateDebitSMSStatus(DebitRecord debitRecord)
        {
            DataBaseOperator dataBaseOperator = null;
            try
            {
                dataBaseOperator = new DataBaseOperator();
                ParamCollections paramCollections = new ParamCollections();

                string sqlStr = "update IFUserDebitRecord set smsSendTimes = @iSmsSendTimes where debitId = @iDebitId";
                paramCollections.Add("@iSmsSendTimes", debitRecord.smsSendTimes + 1);
                paramCollections.Add("@iDebitId", debitRecord.debitId);
                dataBaseOperator.ExecuteStatement(sqlStr, paramCollections.GetParams());
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("SMSSendDao::UpdateDebitSMSStatus", ex.Message);
            }
            finally
            {
                if (null != dataBaseOperator)
                {
                    dataBaseOperator.Close();
                    dataBaseOperator = null;
                }
            }
            return true;
        }
    }
}
