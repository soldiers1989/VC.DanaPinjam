using DBMonoUtility;
using NF.AdminSystem.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using YYLog.ClassLibrary;

namespace NF.AdminSystem.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public class ReportProvider
    {
        public static DataProviderResultModel GetDailyReport(string dateId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select a.debitId,b.ContactName,date_format(releaseLoanTime,'%Y-%m-%d'),round(fee,0), round(actualMoney,0)
                    from IFUserDebitRecord a,IFUserBankInfo b
                    where a.releaseLoanTime >= @dDate1 and a.releaseLoanTime < date_add(@dDate2, interval 1 day) 
                    and a.userId = b.userId and a.status in (-2,1,2,3,4,6);";
                pc.Add("@dDate1", dateId);
                pc.Add("@dDate2", dateId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                result.data = dt;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is ReportProvider::GetDailyReport";
                Log.WriteErrorLog("ReportProvider::GetDailyReport", "获取失败：{0}，异常：{1}", dateId, ex.StackTrace);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel GetDailyPaybackReport(string dateId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select a.debitId,b.ContactName,date_format(releaseLoanTime,'%Y-%m-%d'),round(fee,0) fee1, round(actualMoney,0)
                        ,round(fee,0) fee2,alreadyReturnInterest, returnInterest, alreadyReturnMoney,alreadyReturnInterest + returnInterest + alreadyReturnMoney,userPaybackTime
                    from IFUserDebitRecord a,IFUserBankInfo b,IFUserPayBackDebitRecord c
                    where c.statusTime >= @dDate1 and c.statusTime < date_add(@dDate2, interval 1 day) 
                    and a.debitId = c.DebitId and c.Status = 1 and c.type = 1
                    and a.userId = b.userId and a.status in (-2,1,2,3,4,6) and (alreadyReturnInterest + returnInterest + alreadyReturnMoney) > 0";
                pc.Add("@dDate1", dateId);
                pc.Add("@dDate2", dateId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                result.data = dt;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is ReportProvider::GetDailyPaybackReport";
                Log.WriteErrorLog("ReportProvider::GetDailyPaybackReport", "获取失败：{0}，异常：{1}", dateId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel GetDailyExtendReport(string dateId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select a.debitId,b.ContactName,date_format(releaseLoanTime,'%Y-%m-%d'),round(fee,0) fee1, round(actualMoney,0)
                        ,round(fee,0) fee2,alreadyReturnInterest, returnInterest, alreadyReturnMoney,alreadyReturnInterest + returnInterest + alreadyReturnMoney,userPaybackTime
                    from IFUserDebitRecord a,IFUserBankInfo b,IFUserPayBackDebitRecord c
                    where c.statusTime >= @dDate1 and c.statusTime < date_add(@dDate2, interval 1 day) 
                    and a.debitId = c.DebitId and c.Status = 1 and c.type = 2
                    and a.userId = b.userId and a.status in (-2,1,2,3,4,6) and (alreadyReturnInterest + returnInterest + alreadyReturnMoney) > 0";
                pc.Add("@dDate1", dateId);
                pc.Add("@dDate2", dateId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                result.data = dt;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is ReportProvider::GetDailyExtendReport";
                Log.WriteErrorLog("ReportProvider::GetDailyExtendReport", "获取失败：{0}，异常：{1}", dateId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel GetDailyBadReport(string dateId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select a.debitId,b.ContactName,date_format(releaseLoanTime,'%Y-%m-%d'),round(fee,0), round(actualMoney,0), round(overdueMoney,0)
                    from IFUserDebitRecord a,IFUserBankInfo b
                    where a.releaseLoanTime >= @dDate1 and a.releaseLoanTime < date_add(@dDate2, interval 1 day) 
                    and a.userId = b.userId and a.status = 4 and a.overdueDay > 3";
                pc.Add("@dDate1", dateId);
                pc.Add("@dDate2", dateId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                result.data = dt;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is ReportProvider::GetDailyExtendReport";
                Log.WriteErrorLog("ReportProvider::GetDailyExtendReport", "获取失败：{0}，异常：{1}", dateId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }
    }
}