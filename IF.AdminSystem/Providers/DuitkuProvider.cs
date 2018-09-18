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
        public static DataProviderResultModel CreatePayBack(int userId, int debitId, string type, string merchantCode)
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
                    //重置状态。
                    sqlStr = @"update IFUserPayBackDebitRecord set statusTime=now(),reTryTimes=0,merchantCode=@sMerchantCode
                        where debitId = @iDebitId and status = @iStatus and type = @iType";
                    pc.Add("@sMerchantCode", merchantCode);
                    pc.Add("@iDebitId", debitId);
                    pc.Add("@iStatus", -2);
                    pc.Add("@iType", type);
                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

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
                    sqlStr = @"insert into IFUserPayBackDebitRecord(userId, status,createTime, type, debitId, merchantCode)
                                values(@iUserId, @iStatus,now(), @iType, @iDebitId,@sMerchantCode);";

                    pc.Add("@iUserId", userId);
                    pc.Add("@iStatus", -2);
                    pc.Add("@iType", type);
                    pc.Add("@iDebitId", debitId);
                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                    sqlStr = "select id from IFUserPayBackDebitRecord where debitId = @iDebitId and status = @iStatus and type = @iType order by id desc limit 1";
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

        public static DataProviderResultModel SetDuitkuCallbackRecordStatus(string guid, int status)
        {
            DataProviderResultModel result = new DataProviderResultModel();
            DataBaseOperator dbo = null;
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"update IFDuitkuCallbackLogs set status = @iStatus,statusTime = now() where guid = @sGuid";

                pc.Add("@iStatus", status);
                pc.Add("@sGuid", guid);

                result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SetDuitkuCallbackRecordStatus", "{0}", ex.Message);
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
            IDbConnection conn = null;
            IDbTransaction tran = null;

            try
            {
                dbo = new DataBaseOperator();
                conn = dbo.GetConnection();
                tran = dbo.BeginTransaction(conn);

                ParamCollections pc = new ParamCollections();
                string sqlStr = @"insert into IFDuitkuCallbackLogs(merchantCode,amount,merchantOrderId,productDetail,additionalParam,
                            merchantUserId,reference,signature,issuer_name,issuer_bank,createTime,guid)
                            values(@sMerchantCode,@sAmount,@sMerchantOrderId,@sProductDetail,@sAdditionalParam,
                            @sMerchantUserId,@sReference,@sSignature,@sIssuer_name,@sIssuer_bank,now(),@sGuid);";

                string guid = Guid.NewGuid().ToString();
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
                pc.Add("@sGuid", guid);

                dbo.ExecuteStatement(sqlStr, pc.GetParams());

                result.data = guid;
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                if (null != tran)
                {
                    tran.Rollback();
                }
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SaveDuitkuCallbackRecord", "{0}", ex.Message);
            }
            finally
            {
                if (null != tran)
                {
                    tran.Commit();
                }
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
                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新还款记录表的状态，结果为：{1}", request.merchantOrderId, result.data);

                    sqlStr = @"select date_format(now(),'%Y-%m-%d') now,date_format(payBackDayTime,'%Y-%m-%d') payBackDayTime 
                            from IFUserDebitRecord where debitId = @iDebitId";

                    pc.Add("@iDebitId", debitId);
                    DataTable debitInfo = dbo.GetTable(sqlStr, pc.GetParams(true));
                    if (null != debitInfo && debitInfo.Rows.Count == 1)
                    {
                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]查询到用户应还时间{1}"
                        , request.merchantOrderId, debitInfo.Rows[0]["payBackDayTime"]);

                        //tran = conn.BeginTransaction();
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
                                needPayMoney = (float)Math.Round(extendInfo.extendFee + extendInfo.overdueMoney - extendInfo.partMoney, 0);
                                amoutMoney = (float)Math.Round(amoutMoney, 0);
                                Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "核对应还金额 needPayMoney:{0} - amoutMoney:{1}", needPayMoney, amoutMoney);
                                if (amoutMoney >= needPayMoney)
                                {
                                    #region 全额延期逻辑
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对上，系统自动审核。");
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及最后还款时间。", request.merchantOrderId);
                                    TimeSpan ts = payback.Subtract(now);
                                    if (ts.Days >= 0)
                                    {
                                        sqlStr = @"update IFUserDebitRecord set status = @iStatus,paybackdayTime=date_add(paybackdayTime, interval 7 day)
                                            ,statusTime = now(),partMoney=@fPartMoney,overdueMoney = 0,overdueDay = 0
                                        where debitId = @iDebitId";
                                    }
                                    else
                                    {
                                        sqlStr = @"update IFUserDebitRecord set status = @iStatus,paybackdayTime=date_add(now(), interval 7 day)
                                        ,statusTime = now(),partMoney=@fPartMoney,overdueMoney = 0,overdueDay = 0
                                        where debitId = @iDebitId";
                                    }

                                    pc.Add("@iStatus", 1);
                                    //如果多还了，就暂存到部份还款字段。
                                    pc.Add("@fPartMoney", amoutMoney - needPayMoney);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及最后还款时间 成功({1})。", request.merchantOrderId, dbret);

                                    //如果存在逾期，全额支付后需清算逾期
                                    if (extendInfo.overdueMoney > 0)
                                    {
                                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "逾期费用：{0}，开始清算。", extendInfo.overdueMoney);
                                        sqlStr = @"update IFUserDebitOverdueRecord set clearStatus=@iClearStatus,clearTime=now(),clearSource=@iClearSource 
                                            where debitId=@iDebitId";
                                        pc.Add("@iClearStatus", 1);
                                        pc.Add("@iClearSource", 2);
                                        pc.Add("@iDebitId", debitId);

                                        dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
                                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}][{1}]清算逾期费用 成功({2})。", request.merchantOrderId, debitId, dbret);
                                    }

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                                    values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 2);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 1);
                                    pc.Add("@sAuditDescription", "Proses perpanjangan disetujui(10002).");
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                    #endregion
                                }
                                else
                                {
                                    #region 部份延期逻辑
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对不上，将已还计入部份还款。");
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "应支付：{0}，收到用户支付：{1}，差额：{2}，历史支付：{3}。"
                                    , needPayMoney, amoutMoney, needPayMoney - amoutMoney, extendInfo.partMoney);

                                    sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                                        where id = @iId";
                                    pc.Add("@iStatus1", 1);
                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iId", request.merchantOrderId);
                                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    sqlStr = @"update IFUserDebitRecord set status = if(payBackDayTime < now(), 4, 1)
                                            ,partMoney=ifnull(partMoney,0)+@fMoney,statusTime = now() 
                                        where debitId = @iDebitId";

                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    //取，如果延期成功，下一次还款时间
                                    sqlStr = @"SELECT if(convert(date_format(payBackDayTime,'%Y%m%d'), signed) >= 
                                        convert(date_format(now(),'%Y%m%d'),signed), date_format(date_add(payBackDayTime, interval 7 day),'%Y-%m-%d')
                                        , date_format(date_add(now(), interval 7 day),'%Y-%m-%d')
                                        ) extendNextPayback FROM IFUserDebitRecord where debitId = @iDebitId;";
                                    pc.Add("@iDebitId", debitId);
                                    object extendNextPayback = dbo.GetScalar(sqlStr, pc.GetParams(true));

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                            values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";
                                    //还款所需金额 ＝ 贷款金额 ＋ 逾期费 － 本次支付 － 历史支付金额
                                    float needPaybackMoney = extendInfo.debitMoney + extendInfo.overdueMoney - amoutMoney - extendInfo.partMoney;
                                    //已支付Rp100.000，还款还需要支付Rp140.000，延期到2018-09-21还需要支付Rp400.000
                                    //Sudah bayar 100 harus d bayarkan 1.400.000 sepenuhnya dibayar dan untuk diperpanjang sampai 21 -09- 2018 harus bayar 400.000
                                    pc.Add("@iAuditType", 2);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 1);
                                    pc.Add("@sAuditDescription", String.Format("Sudah bayar {0} harus dibayarkan {1} sepenuhnya dan untuk diperpanjang sampai {2} harus bayar {3}.\r\nJika ada pertanyaan silakan hubungi:\r\n0813 1682 3995\r\n0813 8366 2454."
                                        , amoutMoney.ToString("N0").Replace(",", "."), needPaybackMoney.ToString("N0").Replace(",", "."), extendNextPayback, (needPayMoney - amoutMoney).ToString("N0").Replace(",", ".")));//延期申请（审核中）
                                    pc.Add("@iUserId", -1);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                    #endregion
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
                                needPayMoney = (float)Math.Round(debitModel.payBackMoney, 0);
                                amoutMoney = (float)Math.Round(amoutMoney, 0);
                                Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "核对应还金额 needPayMoney:{0} - amoutMoney:{1}", needPayMoney, amoutMoney);
                                if (amoutMoney >= needPayMoney)
                                {
                                    #region 全额还款逻辑
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对上，进入系统审核。");

                                    sqlStr = @"update IFUserDebitRecord set status = @iStatus,userPaybackTime=now(),statusTime = now(),
                                            partMoney=@fPartMoney,overdueMoney = 0,overdueDay = 0
                                            where debitId = @iDebitId";
                                    pc.Add("@iStatus", 3);
                                    //如果多还了，暂存到该字段。
                                    pc.Add("@fPartMoney", amoutMoney - needPayMoney);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    //如果存在逾期，全额支付后需清算逾期
                                    if (debitModel.overdueMoney > 0)
                                    {
                                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "逾期费用：{0}，开始清算。", debitModel.overdueMoney);
                                        sqlStr = @"update IFUserDebitOverdueRecord set clearStatus=@iClearStatus,clearTime=now(),clearSource=@iClearSource 
                                            where debitId=@iDebitId";
                                        pc.Add("@iClearStatus", 1);
                                        pc.Add("@iClearSource", 2);
                                        pc.Add("@iDebitId", debitId);

                                        dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
                                        Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}][{1}]清算逾期费用 成功({2})。", request.merchantOrderId, debitId, dbret);
                                    }

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及还款时间 成功({1})。", request.merchantOrderId, dbret);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                            values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 3);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 3);
                                    pc.Add("@sAuditDescription", "Proses pembayaran disetujui(10002).");
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                    #endregion
                                }
                                else
                                {
                                    #region 部份还款逻辑
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "金额对不上，进入人工审核。");
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "应支付：{0}，收到用户支付：{1}，差额：{2}，历史支付：{3}。"
                                    , needPayMoney, amoutMoney, needPayMoney - amoutMoney, debitModel.partMoney);

                                    sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                                        where id = @iId";
                                    pc.Add("@iStatus1", 1);
                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iId", request.merchantOrderId);
                                    result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    //原来是什么状态，就更新为什么状态,将还款的钱更新到部份还款金额字段。
                                    sqlStr = @"update IFUserDebitRecord set status = if(payBackDayTime < now(), 4, 1)
                                                    ,partMoney=ifnull(partMoney,0)+@fMoney,statusTime = now() 
                                                where debitId = @iDebitId";
                                    //pc.Add("@iStatus", 1);
                                    pc.Add("@fMoney", request.amount);
                                    pc.Add("@iDebitId", debitId);
                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);

                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]更新贷款记录表状态及还款时间 成功({1})。", request.merchantOrderId, dbret);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]开始插入审核记录。", request.merchantOrderId);

                                    sqlStr = @"insert into IFUserAduitDebitRecord(AduitType,debitId,status,description,adminId,auditTime)
	                                    values(@iAuditType, @iDebitId, @iAuditStatus, @sAuditDescription, @iUserId, now());";

                                    pc.Add("@iAuditType", 3);
                                    pc.Add("@iDebitId", debitId);
                                    pc.Add("@iAuditStatus", 1);
                                    pc.Add("@sAuditDescription", String.Format("Sudah membayar Rp {0}.\r\nuntuk pelunasan silakan bayarkan kembali sisanya sebesar Rp {1}.\r\nJika ada pertanyaan silakan hubungi:\r\n0813 1682 3995\r\n0813 8366 2454."
                                    , amoutMoney.ToString("N0").Replace(",", "."), (debitModel.payBackMoney - amoutMoney).ToString("N0").Replace(",", ".")));
                                    pc.Add("@iUserId", -1);

                                    dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true), conn);
                                    Log.WriteDebugLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "[{0}]插入审核记录 成功({1})。", request.merchantOrderId, dbret);
                                    #endregion
                                }
                            }
                        }
                        result.result = Result.SUCCESS;
                        tran.Commit();
                    }
                    else
                    {
                        result.result = Result.ERROR;
                        Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "查找贷款ID失败，记录不存在。{0}", debitId);
                        tran.Rollback();
                    }
                }
                else
                {
                    result.result = Result.ERROR;
                    Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "根据订单ID查找贷款ID失败，有可能该订单已处理。{0}", request.merchantOrderId);
                }
            }
            catch (Exception ex)
            {
                if (null != tran)
                    tran.Rollback();
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "{0} 订单在执行时发生异常： {1}", request.merchantOrderId, ex.Message);
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
            return result;
        }
    }
}