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
    public class DebitProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="debitMoney"></param>
        /// <param name="debitPeroid"></param>
        /// <returns></returns>
        public static DataProviderResultModel GetInterestRateByDebitStyle(float debitMoney, int debitPeriod)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = "select interestRate,overdueDayInterest from IFDebitStyle where money = @dDebitMoney and period = @iDebitPeriod;";
                pc.Add("@dDebitMoney", debitMoney);
                pc.Add("@iDebitPeriod", debitPeriod);

                Hashtable table = new Hashtable();
                DataRow rates = dbo.GetRow(sqlStr, pc.GetParams());
                if (null != rates)
                {
                    result.result = Result.SUCCESS;
                    float rate = 0f;
                    float overdueRate = 0f;
                    float.TryParse(Convert.ToString(rates[0]), out rate);
                    float.TryParse(Convert.ToString(rates[1]), out overdueRate);
                    result.data = new List<float> { rate, overdueRate };
                }
                else
                {
                    result.result = MainErrorModels.NO_SUCH_DEBIT_COMBINATION;
                    result.message = "There is no such combination.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("DebitProvider::GetInterestRateByDebitStyle", "获取失败：{0},{1}，异常：{2}", debitMoney, debitPeriod, ex.Message);
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitMoney"></param>
        /// <param name="debitPeroid"></param>
        /// <param name="bankId"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static DataProviderResultModel SubmitDebitReuqest(int userId, float debitMoney, int debitPeroid, int bankId, string description)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iUserId", userId);
                pc.Add("@dDebitMoney", debitMoney);
                pc.Add("@iDebitPeroid", debitPeroid);
                pc.Add("@iBankId", bankId);
                pc.Add("@sDescription", description);

                Hashtable table = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_debit_submitrequest", pc.GetParams(), out table);
                if (null != dt && dt.Rows.Count == 1)
                {

                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);

                    if (result.result < 0)
                    {
                        result.message = Convert.ToString(dt.Rows[0][1]);
                        result.data = new { debitRecordId  = -1};
                        Log.WriteErrorLog("DebitProvider::SubmitDebitReuqest", "提交申请失败：{0}|{1}|{2}|{3}|{4},结果是：{5}", userId, debitMoney, debitPeroid, bankId, description, dt.Rows[0][1]);
                    }
                    else
                    {
                        result.result = Result.SUCCESS;
                        ///记录ID
                        result.data = new { debitRecordId = Convert.ToString(dt.Rows[0][2]) };
                    }
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "error from the submit debit request.";
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is SubmitDebitReuqest";
                Log.WriteErrorLog("DebitProvider::GetUserBankInfo", "获取失败：{0}，异常：{1}", userId, ex.Message);
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

        public static DataProviderResultModel GetUserDebitRecords(int userId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                //限制返回记录条数。
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select debitId,userId, debitMoney,ifnull(partMoney,0) partMoney, Status, date_format(createTime, '%Y-%m-%d') createTime, description,ifnull(overdueMoney, 0) overdueMoney,ifnull(overdueDay,0) overdueDay, bankId,date_format(releaseLoanTime, '%Y-%m-%d') releaseLoanTime,date_format(payBackDayTime, '%Y-%m-%d') payBackDayTime, 
certificate, date_format(statusTime, '%Y-%m-%d') statusTime, debitPeroid, payBackMoney,(select b.Description from IFUserAduitDebitRecord b where b.debitId = a.DebitId order by id desc limit 1) auditInfo,
(select if(a.Status = 4, overdueDayInterest,b.interestRate)*a.DebitMoney from IFDebitStyle b where b.money = a.DebitMoney and b.period = a.DebitPeroid) dayInterset
                    from IFUserDebitRecord a where userId = @iUserId order by DebitId desc limit 10;";
                pc.Add("@iUserId", userId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DebitInfoModel info = new DebitInfoModel();
                        info.userId = userId;
                        int.TryParse(Convert.ToString(dt.Rows[i]["debitId"]), out info.debitId);
                        int.TryParse(Convert.ToString(dt.Rows[i]["overdueDay"]), out info.overdueDay);
                        float.TryParse(Convert.ToString(dt.Rows[i]["dayInterset"]), out info.dayInterset);
                        float.TryParse(Convert.ToString(dt.Rows[i]["debitMoney"]), out info.debitMoney);
                        float.TryParse(Convert.ToString(dt.Rows[i]["partMoney"]), out info.partMoney);
                        float.TryParse(Convert.ToString(dt.Rows[i]["overdueMoney"]), out info.overdueMoney);
                        int.TryParse(Convert.ToString(dt.Rows[i]["Status"]), out info.status);
                        int.TryParse(Convert.ToString(dt.Rows[i]["bankId"]), out info.bankId);
                        int.TryParse(Convert.ToString(dt.Rows[i]["debitPeroid"]), out info.debitPeroid);
                        float.TryParse(Convert.ToString(dt.Rows[i]["payBackMoney"]), out info.payBackMoney);
                        info.createTime = Convert.ToString(dt.Rows[i]["createTime"]);
                        info.description = Convert.ToString(dt.Rows[i]["description"]);
                        info.certificate = Convert.ToString(dt.Rows[i]["certificate"]);

                        info.payBackMoney = info.payBackMoney + info.overdueMoney - info.partMoney;
                        info.releaseLoanTime = Convert.ToString(dt.Rows[i]["releaseLoanTime"]);
                        info.auditTime = Convert.ToString(dt.Rows[i]["statusTime"]);
                        info.repaymentTime = Convert.ToString(dt.Rows[i]["payBackDayTime"]);
                        info.auditInfo = Convert.ToString(dt.Rows[i]["auditInfo"]);
                        infos.Add(info);
                    }
                    result.data = infos;
                }
                else
                {
                    result.result = Result.SUCCESS;
                    result.data = infos;
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is DebitProvider::GetUserDebitRecords";
                Log.WriteErrorLog("DebitProvider::GetUserDebitRecords", "获取失败：{0}|{1}，异常：{2}", userId, ex.Message);
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DataProviderResultModel GetUserDebitAttention(int userId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select debitId,userId, debitMoney, Status, date_format(createTime, '%Y-%m-%d') createTime, description,ifnull(overdueMoney, 0) overdueMoney,ifnull(overdueDay,0) overdueDay, bankId,date_format(releaseLoanTime, '%Y-%m-%d') releaseLoanTime,date_format(payBackDayTime, '%Y-%m-%d') payBackDayTime, 
certificate, date_format(statusTime, '%Y-%m-%d') statusTime, debitPeroid, payBackMoney,(select b.Description from IFUserAduitDebitRecord b where b.debitId = a.DebitId order by id desc limit 1) auditInfo,
(select if(a.Status = 4, overdueDayInterest,b.interestRate)*a.DebitMoney from IFDebitStyle b where b.money = a.DebitMoney and b.period = a.DebitPeroid) dayInterset
                    from IFUserDebitRecord a where userId = @iUserId and status in (-2,1,2,4) limit 1;";
                pc.Add("@iUserId", userId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                DebitInfoModel info = new DebitInfoModel();
                if (null != dt && dt.Rows.Count > 0)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0]["userId"]), out info.userId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitId"]), out info.debitId);
                    float.TryParse(Convert.ToString(dt.Rows[0]["dayInterset"]), out info.dayInterset);
                    float.TryParse(Convert.ToString(dt.Rows[0]["overdueMoney"]), out info.overdueMoney);
                    float.TryParse(Convert.ToString(dt.Rows[0]["debitMoney"]), out info.debitMoney);
                    int.TryParse(Convert.ToString(dt.Rows[0]["Status"]), out info.status);
                    int.TryParse(Convert.ToString(dt.Rows[0]["bankId"]), out info.bankId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitPeroid"]), out info.debitPeroid);
                    float.TryParse(Convert.ToString(dt.Rows[0]["payBackMoney"]), out info.payBackMoney);
                    info.createTime = Convert.ToString(dt.Rows[0]["createTime"]);
                    info.description = Convert.ToString(dt.Rows[0]["description"]);
                    info.releaseLoanTime = Convert.ToString(dt.Rows[0]["releaseLoanTime"]);
                    info.auditTime = Convert.ToString(dt.Rows[0]["statusTime"]);
                    info.repaymentTime = Convert.ToString(dt.Rows[0]["payBackDayTime"]);

                    info.certificate = Convert.ToString(dt.Rows[0]["certificate"]);
                    info.auditInfo = Convert.ToString(dt.Rows[0]["auditInfo"]);
                }
                else
                {
                    result.result = Result.SUCCESS;
                }
                result.data = info;
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is DebitProvider::GetUserDebitAttention";
                Log.WriteErrorLog("DebitProvider::GetUserDebitRecords", "获取失败：{0}|{1}，异常：{2}", userId, ex.Message);
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

        public static DataProviderResultModel GetUserDebitRecord(int debitId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select debitId,userId, debitMoney, ifnull(partMoney,0) partMoney, Status, date_format(createTime, '%Y-%m-%d') createTime, description,ifnull(overdueMoney, 0) overdueMoney,ifnull(overdueDay,0) overdueDay, bankId,date_format(releaseLoanTime, '%Y-%m-%d') releaseLoanTime,date_format(payBackDayTime, '%Y-%m-%d') payBackDayTime, 
certificate, date_format(statusTime, '%Y-%m-%d') statusTime, debitPeroid, payBackMoney,(select b.Description from IFUserAduitDebitRecord b where b.debitId = a.DebitId order by id desc limit 1) auditInfo,
(select if(a.Status = 4, overdueDayInterest,b.interestRate)*a.DebitMoney from IFDebitStyle b where b.money = a.DebitMoney and b.period = a.DebitPeroid) dayInterset
                    from IFUserDebitRecord a where DebitId = @iDebitId";
                pc.Add("@iDebitId", debitId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    DebitInfoModel info = new DebitInfoModel();
                    int.TryParse(Convert.ToString(dt.Rows[0]["userId"]), out info.userId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitId"]), out info.debitId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["overdueDay"]), out info.overdueDay);
                    float.TryParse(Convert.ToString(dt.Rows[0]["dayInterset"]), out info.dayInterset);
                    float.TryParse(Convert.ToString(dt.Rows[0]["overdueMoney"]), out info.overdueMoney);
                    float.TryParse(Convert.ToString(dt.Rows[0]["partMoney"]), out info.partMoney);
                    float.TryParse(Convert.ToString(dt.Rows[0]["debitMoney"]), out info.debitMoney);
                    int.TryParse(Convert.ToString(dt.Rows[0]["Status"]), out info.status);
                    int.TryParse(Convert.ToString(dt.Rows[0]["bankId"]), out info.bankId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitPeroid"]), out info.debitPeroid);
                    float.TryParse(Convert.ToString(dt.Rows[0]["payBackMoney"]), out info.payBackMoney);

                    info.payBackMoney = info.payBackMoney + info.overdueMoney - info.partMoney;
                    info.createTime = Convert.ToString(dt.Rows[0]["createTime"]);
                    info.description = Convert.ToString(dt.Rows[0]["description"]);
                    info.releaseLoanTime = Convert.ToString(dt.Rows[0]["releaseLoanTime"]);
                    info.auditTime = Convert.ToString(dt.Rows[0]["statusTime"]);
                    info.repaymentTime = Convert.ToString(dt.Rows[0]["payBackDayTime"]);
                    
                    info.certificate = Convert.ToString(dt.Rows[0]["certificate"]);
                    info.auditInfo = Convert.ToString(dt.Rows[0]["auditInfo"]);
                    result.data = info;
                }
                else
                {
                    result.result = Result.SUCCESS;
                    result.data = new DebitInfoModel();
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is DebitProvider::GetUserDebitRecord";
                Log.WriteErrorLog("DebitProvider::GetUserDebitRecord", "获取失败：{0}，异常：{1}", debitId, ex.Message);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="debitId"></param>
        /// <returns></returns>
        public static DataProviderResultModel GetUserExtendRecord(int debitId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select debitId,userId, debitMoney, ifnull(partMoney,0) partMoney, Status, date_format(createTime, '%Y-%m-%d') createTime, description,ifnull(overdueMoney, 0) overdueMoney,ifnull(overdueDay,0) overdueDay, bankId,date_format(releaseLoanTime, '%Y-%m-%d') releaseLoanTime,date_format(payBackDayTime, '%Y-%m-%d') payBackDayTime, 
certificate, date_format(statusTime, '%Y-%m-%d') statusTime, debitPeroid, payBackMoney,(select b.Description from IFUserAduitDebitRecord b where b.debitId = a.DebitId order by id desc limit 1) auditInfo,
(select if(a.Status = 4, overdueDayInterest,b.interestRate)*a.DebitMoney from IFDebitStyle b where b.money = a.DebitMoney and b.period = a.DebitPeroid) dayInterset
                    from IFUserDebitRecord a where DebitId = @iDebitId";
                pc.Add("@iDebitId", debitId);
                DebitExtendModel extend = new DebitExtendModel();
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    DebitInfoModel info = new DebitInfoModel();
                    int.TryParse(Convert.ToString(dt.Rows[0]["userId"]), out info.userId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitId"]), out info.debitId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["overdueDay"]), out info.overdueDay);
                    float.TryParse(Convert.ToString(dt.Rows[0]["dayInterset"]), out info.dayInterset);
                    float.TryParse(Convert.ToString(dt.Rows[0]["overdueMoney"]), out info.overdueMoney);
                    float.TryParse(Convert.ToString(dt.Rows[0]["partMoney"]), out info.partMoney);
                    float.TryParse(Convert.ToString(dt.Rows[0]["debitMoney"]), out info.debitMoney);
                    int.TryParse(Convert.ToString(dt.Rows[0]["Status"]), out info.status);
                    int.TryParse(Convert.ToString(dt.Rows[0]["bankId"]), out info.bankId);
                    int.TryParse(Convert.ToString(dt.Rows[0]["debitPeroid"]), out info.debitPeroid);
                    float.TryParse(Convert.ToString(dt.Rows[0]["payBackMoney"]), out info.payBackMoney);
                    info.createTime = Convert.ToString(dt.Rows[0]["createTime"]);
                    info.description = Convert.ToString(dt.Rows[0]["description"]);
                    info.releaseLoanTime = Convert.ToString(dt.Rows[0]["releaseLoanTime"]);
                    info.auditTime = Convert.ToString(dt.Rows[0]["statusTime"]);
                    info.repaymentTime = Convert.ToString(dt.Rows[0]["payBackDayTime"]);

                    info.certificate = Convert.ToString(dt.Rows[0]["certificate"]);
                    info.auditInfo = Convert.ToString(dt.Rows[0]["auditInfo"]);

                    if (info.status != 1 && info.status != 4 && info.status != -2)
                    {
                        result.result = MainErrorModels.THE_DEBIT_RECORD_STATUS_FAIL;
                        result.message = "Only within loan, overdue, payback failed , can request extend";
                        return result;
                    }
                    extend.userId = info.userId;
                    extend.debitId = info.debitId;
                    extend.status = info.status;
                    extend.partMoney = info.partMoney;
                    extend.debitMoney = info.debitMoney;
                    extend.debitPeroid = info.debitPeroid;
                    extend.overdueMoney = info.overdueMoney;

                    DataProviderResultModel rateResult = DebitProvider.GetInterestRateByDebitStyle(info.debitMoney, info.debitPeroid);

                    if (null != rateResult.data)
                    {
                        List<float> rate = rateResult.data as List<float>;

                        if(rate[0] > 1)
                        {
                            extend.extendFee = rate[0];
                        }
                        else
                        {
                           extend.extendFee = rate[0] * info.debitMoney;
                        }
                    }
                }
                else
                {
                    result.result = Result.SUCCESS;
                }

                result.data = extend;

                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is DebitProvider::GetUserDebitRecord";
                Log.WriteErrorLog("DebitProvider::GetUserDebitRecord", "获取失败：{0}，异常：{1}", debitId, ex.Message);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitId"></param>
        /// <param name="payBackDebitMoney"></param>
        /// <param name="certificateUrl"></param>
        /// <returns></returns>
        public static DataProviderResultModel ExtendDebitRequest(int userId, int debitId, float payBackDebitMoney, string certificateUrl)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iUserId", userId);
                pc.Add("@iDebitId", debitId);
                pc.Add("@dPayBackDebitMoney", payBackDebitMoney);
                pc.Add("@sCertificateUrl", certificateUrl);

                Hashtable table = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_debit_extendrequest", pc.GetParams(), out table);
                if (null != dt && dt.Rows.Count == 1)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    result.message = Convert.ToString(dt.Rows[0][1]);
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "error from the submit extend debit request.";
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("DebitProvider::ExtendDebitRequest", "获取失败：{0}|{1}，异常：{2}", userId, ex.Message);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitId"></param>
        /// <param name="payBackDebitMoney"></param>
        /// <param name="certificateUrl"></param>
        /// <returns></returns>
        public static DataProviderResultModel PayBackDebitRequest(int userId, int debitId, float payBackDebitMoney, string certificateUrl)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            List<DebitInfoModel> infos = new List<DebitInfoModel>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iUserId", userId);
                pc.Add("@iDebitId", debitId);
                pc.Add("@dPayBackDebitMoney", payBackDebitMoney);
                pc.Add("@sCertificateUrl", certificateUrl);

                Hashtable table = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_debit_paybackrequest", pc.GetParams(), out table);
                if (null != dt && dt.Rows.Count == 1)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    result.message = Convert.ToString(dt.Rows[0][1]);
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "error from the submit payback debit request.";
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("DebitProvider::PayBackDebitRequest", "获取失败：{0}|{1}，异常：{2}", userId, ex.Message);
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


        public static PayBackDebitModel GetDebitPayBackRecord(int userId, int debitId)
        {
            DataBaseOperator dbo = null;
            PayBackDebitModel info = new PayBackDebitModel();

            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select userId,PayBackDebitMoney,status,createtime,statusTime,certificateUrl,debitId 
                        from IFUserPayBackDebitRecord where userId = @iUserId and debitId = @iDebitId;";
                pc.Add("@iUserId", userId);
                pc.Add("@iDebitId", debitId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    float.TryParse(Convert.ToString(dt.Rows[0]["payBackDebitMoney"]), out info.payBackDebitMoney);
                    int.TryParse(Convert.ToString(dt.Rows[0]["Status"]), out info.status);
                    info.userId = userId;
                    info.debitId = debitId;
                    info.certificateUrl = Convert.ToString(dt.Rows[0]["certificateUrl"]);
                    info.statusTime = Convert.ToString(dt.Rows[0]["statusTime"]);
                }
                return info;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("DebitProvider::GetDebitPayBackRecord", "获取失败：{0}|{1}，异常：{2}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return info;
        }
    }
}