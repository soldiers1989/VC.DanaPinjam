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
    public class DuitkuProvider
    {
        public static DataProviderResultModel CreatePayBack(int userId, int debitId, string type)
        {
            DataProviderResultModel result = new DataProviderResultModel();
            DataBaseOperator dbo = null;

            try
            {
                dbo = new DataBaseOperator();
                string sqlStr = "select count(1) from IFUserPayBackDebitRecord where debitId = @iDebitId and status = @iStatus and type = @iType";
                ParamCollections pc = new ParamCollections();
                pc.Add("@iDebitId", debitId);
                pc.Add("@iStatus", -2);
                pc.Add("@iType", type);

                int count = dbo.GetCount(sqlStr, pc.GetParams(true));
                if (count > 0)
                {
                    sqlStr = "select id from IFUserPayBackDebitRecord where debitId = @iDebitId and status = @iStatus and type = @iType";
                    pc.Add("@iDebitId", debitId);
                    pc.Add("@iStatus", -2);
                    pc.Add("@iType", type);

                    object obj = dbo.GetScalar(sqlStr, pc.GetParams(true));
                    result.data = obj;
                    result.result = Result.SUCCESS;
                }
                else
                {
                    sqlStr = @"insert into IFUserPayBackDebitRecord(userId, status,createTime, type, debitId)
                                values(@iUserId, @iStatus,now(), @iType, @iDebitId);";

                    pc.Add("@iUserId", userId);
                    pc.Add("@iStatus", -2);
                    pc.Add("@iType", type);
                    pc.Add("@iDebitId", debitId);
                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                    sqlStr = "select id from IFUserPayBackDebitRecord where debitId = @iDebitId and status = @iStatus and type = @iType";
                    pc.Add("@iDebitId", debitId);
                    pc.Add("@iStatus", -2);
                    pc.Add("@iType", type);

                    object obj = dbo.GetScalar(sqlStr, pc.GetParams(true));
                    result.data = obj;
                    result.result = Result.SUCCESS;
                }
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                Log.WriteErrorLog("DuitkuProvider::CreatePayBack", ex.Message);
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

        public static DataProviderResultModel SaveDuitkuCallbackRecord(CallbackRequestModel request)
        {
            DataProviderResultModel result = new DataProviderResultModel();
            DataBaseOperator dbo = null;
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"insert into IFDuitkuCallbackLogs(merchantCode,amount,merchantOrderId,productDetail,additionalParam,
                            merchantUserId,reference,signature,issuer_name,issuer_bank,createTime)
                            values(@sMerchantCode,@sAmount,@sMerchantOrderId,@sProductDetail,@sAdditionalParam,
                            @sMerchantUserId,@sReference,@sSignature,@sIssuer_name,@sIssuer_bank,now());";

                pc.Add("@sMerchantCode", request.merchantCode);
                pc.Add("@sAmount", request.amount);
                pc.Add("@sMerchantOrderId", request.merchantOrderId);
                pc.Add("@sProductDetail", request.productDetail);
                pc.Add("@sAdditionalParam", request.additionalParam);
                pc.Add("@sMerchantUserId", request.merchantUserId);
                pc.Add("@sReference", request.reference);
                pc.Add("@sSignature", request.signature);
                pc.Add("@sIssuer_name", request.issuer_name);
                pc.Add("@sIssuer_bank", request.issuer_bank);

                result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SaveDuitkuCallbackRecord", "{0}", ex.Message);
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

        public static DataProviderResultModel SetDuitkuPaybackRecordStaus(CallbackRequestModel request)
        {
            DataBaseOperator dbo = null;
            IDbConnection conn = null;
            IDbTransaction tran = null;
            int dbret = -1;

            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();

                string sqlStr = "select debitId,type,userId from IFUserPayBackDebitRecord where id = @iId and status = @iStatus";
                ParamCollections pc = new ParamCollections();
                pc.Add("@iId", request.merchantOrderId);
                pc.Add("@iStatus", -2);

                Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]查询还款记录表的数据，用户、还款类型、贷款记录ID。", request.merchantOrderId);

                DataTable paybackInfo = dbo.GetTable(sqlStr, pc.GetParams(true));
                if (null != paybackInfo && paybackInfo.Rows.Count == 1)
                {
                    string debitId = Convert.ToString(paybackInfo.Rows[0]["debitId"]);
                    string type = Convert.ToString(paybackInfo.Rows[0]["type"]);
                    string userId = Convert.ToString(paybackInfo.Rows[0]["userId"]);

                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]查询到还款记录，用户：{1}、还款类型:{2}、贷款记录ID:{3}。"
                    , request.merchantOrderId, userId, type, debitId);

                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始更新还款记录表的状态", request.merchantOrderId);

                    conn = dbo.GetConnection();
                    tran = dbo.BeginTransaction(conn);

                    sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                    where id = @iId and status = @iStatus2";
                    pc.Add("@iStatus1", 1);
                    pc.Add("@fMoney", request.amount);
                    pc.Add("@iId", request.merchantOrderId);
                    pc.Add("@iStatus2", -2);
                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                    tran.Commit();

                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新还款记录表的状态，结果为：{1}", request.merchantOrderId, result.data);

                    sqlStr = @"select date_format(now(),'%Y-%m-%d') now,date_format(payBackDayTime,'%Y-%m-%d') payBackDayTime 
                            from IFUserDebitRecord where debitId = @iDebitId";

                    pc.Add("@iDebitId", debitId);
                    DataTable debitInfo = dbo.GetTable(sqlStr, pc.GetParams(true));
                    if (null != debitInfo && debitInfo.Rows.Count == 1)
                    {
                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]查询到用户应还时间{1}"
                        , request.merchantOrderId, debitInfo.Rows[0]["payBackDayTime"]);

                        tran = conn.BeginTransaction();
                        ///3 － 延期；4 － 还款
                        if (type == "3")
                        {
                            //如果还款日期大于当前日期，延期后的时间是从还款时间＋7天（相当于提前延期）。
                            //如果还款日期小于当前日期，延期时间是从当天＋7天。
                            DateTime now = DateTime.Now;
                            DateTime payback = DateTime.Now;

                            DateTime.TryParse(Convert.ToString(debitInfo.Rows[0]["payBackDayTime"]), out payback);
                            DateTime.TryParse(Convert.ToString(debitInfo.Rows[0]["now"]), out now);

                            DataProviderResultModel extendResult = DebitProvider.GetUserExtendRecord(Convert.ToInt32(debitId));
                            if (extendResult.result == Result.SUCCESS)
                            {
                                DebitExtendModel extendInfo = extendResult.data as DebitExtendModel;
                                float amoutMoney = 0f;
                                float needPayMoney = 0f;

                                float.TryParse(request.amount, out amoutMoney);
                                needPayMoney = (float)Math.Round(extendInfo.extendFee + extendInfo.overdueMoney, 0);
                                amoutMoney = (float)Math.Round(amoutMoney, 0);
                                Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "核对应还金额 needPayMoney:{0} - amoutMoney:{1}", needPayMoney, amoutMoney);
                                if (amoutMoney >= needPayMoney)
                                {
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对上，系统自动审核。");
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及最后还款时间。", request.merchantOrderId);
                                    TimeSpan ts = payback.Subtract(now);
                                    if (ts.Days >= 0)
                                    {
                                        sqlStr = "update IFUserDebitRecord set status = @iStatus,paybackdayTime=date_add(paybackdayTime, interval 7 day),statusTime = now() where debitId = @iDebitId";
                                    }
                                    else
                                    {
                                        sqlStr = "update IFUserDebitRecord set status = @iStatus,paybackdayTime=date_add(now(), interval 7 day),statusTime = now() where debitId = @iDebitId";
                                    }

                                    pc.Add("@iStatus", 1);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及最后还款时间 成功({1})。", request.merchantOrderId, dbret);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                                    values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 2);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 1);
                                    pc.Add("@sAuditDescription", "extend success.");
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                }
                                else
                                {
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对不上，进入人工审核。");
                                    sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                                        where id = @iId";
                                    pc.Add("@iStatus1", 0);
                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iId", request.merchantOrderId);
                                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    sqlStr = "update IFUserDebitRecord set status = @iStatus,statusTime = now() where debitId = @iDebitId";
                                    pc.Add("@iStatus", 6);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                            values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 2);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 0);
                                    pc.Add("@sAuditDescription", "auditing.");
                                    pc.Add("@iUserId", -1);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                }
                            }
                        }
                        if (type == "4")
                        {
                            DataProviderResultModel debitResult = DebitProvider.GetUserDebitRecord(Convert.ToInt32(debitId));
                            if (debitResult.result == Result.SUCCESS)
                            {
                                DebitInfoModel debitModel = debitResult.data as DebitInfoModel;
                                float amoutMoney = 0f;
                                float needPayMoney = 0f;

                                float.TryParse(request.amount, out amoutMoney);
                                needPayMoney = (float)Math.Round(debitModel.payBackMoney + debitModel.overdueMoney, 0);
                                amoutMoney = (float)Math.Round(amoutMoney, 0);
                                Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "核对应还金额 needPayMoney:{0} - amoutMoney:{1}", needPayMoney, amoutMoney);
                                if (amoutMoney >= needPayMoney)
                                {
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对上，进入系统审核。");

                                    sqlStr = "update IFUserDebitRecord set status = @iStatus,userPaybackTime=now(),statusTime = now() where debitId = @iDebitId";
                                    pc.Add("@iStatus", 3);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及还款时间 成功({1})。", request.merchantOrderId, dbret);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                            values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 3);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 3);
                                    pc.Add("@sAuditDescription", "payback success.");
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                }
                                else
                                {
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对不上，进入人工审核。");
                                    sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                                        where id = @iId";
                                    pc.Add("@iStatus1", 0);
                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iId", request.merchantOrderId);
                                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    sqlStr = "update IFUserDebitRecord set status = @iStatus,userPaybackTime=now(),statusTime = now() where debitId = @iDebitId";
                                    pc.Add("@iStatus", 2);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及还款时间 成功({1})。", request.merchantOrderId, dbret);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                                    values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 3);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 0);
                                    pc.Add("@sAuditDescription", "payback auditing.");
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                }
                            }
                            tran.Commit();
                        }
                    }
                    else
                    {
                        Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "查找贷款ID失败，记录不存在。{0}", debitId);    
                    }
                }
                else
                {
                    Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "根据订单ID查找贷款ID失败，有可能该订单已处理。{0}", request.merchantOrderId);
                }
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                try
                {
                    if (null != tran)
                    {
                        tran.Rollback();
                    }
                }
                catch { }
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "{0} 订单在执行时发生异常： {1}", request.merchantOrderId, ex.Message);
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