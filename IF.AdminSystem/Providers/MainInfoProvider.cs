using DBMonoUtility;
using NF.AdminSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using YYLog.ClassLibrary;

namespace NF.AdminSystem.Providers
{
    public class MainInfoProvider
    {
        public static List<SelectionModel> GetSelection()
        {
            DataBaseOperator dbo = null;
            List<SelectionModel> infos = new List<SelectionModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select selectName,selectValue,selectType from IFSelection where status = @iStatus order by selectType, orderIndex desc";
                pc.Add("@iStatus", 1);
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        SelectionModel info = new SelectionModel();
                        info.selectText = Convert.ToString(dt.Rows[i]["selectName"]);
                        int.TryParse(Convert.ToString(dt.Rows[i]["selectValue"]), out info.selectValue);
                        info.selectType = Convert.ToString(dt.Rows[i]["selectType"]);
                        infos.Add(info);
                    }
                }
                return infos;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("MainInfoProvider::GetSelection", "获取失败，异常：{0}", ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return infos;
        }

        public static DataProviderResultModel GetNotices()
        {
            DataBaseOperator dbo = null;
            List<NoticeModel> infos = new List<NoticeModel>();
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select title,content from IFNotice where startTime < now() and endTime > now() order by id desc";
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        NoticeModel info = new NoticeModel();
                        info.content = Convert.ToString(dt.Rows[i]["content"]);
                        info.title = Convert.ToString(dt.Rows[i]["title"]);
                        
                        infos.Add(info);
                    }
                }

                result.result = Result.SUCCESS;
                result.data = infos;
                return result;
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                result.message = "The database logic error.The function is MainInfoProvider::GetNotices";
                Log.WriteErrorLog("MainInfoProvider::GetNotices", "获取失败，异常：{0}", ex.Message);
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

        public static DataProviderResultModel GetBankCodes()
        {
            DataBaseOperator dbo = null;
            List<BankCode> infos = new List<BankCode>();
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select bankCode,bankName from IFBanksCode where status = 1 order by bankName";
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        BankCode info = new BankCode();
                        info.bankCode = Convert.ToString(dt.Rows[i]["bankCode"]);
                        info.bankName = Convert.ToString(dt.Rows[i]["bankName"]);
                        
                        infos.Add(info);
                    }
                }

                result.result = Result.SUCCESS;
                result.data = infos;
                return result;
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                result.message = "The database logic error.The function is MainInfoProvider::GetBankCodes";
                Log.WriteErrorLog("MainInfoProvider::GetBankCodes", "获取失败，异常：{0}", ex.Message);
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