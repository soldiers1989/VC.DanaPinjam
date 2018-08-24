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
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                string sqlStr = @"update IFUserPayBackDebitRecord set status = @iStatus1,statusTime = now(),money=@fMoney 
                    where id = @iId and status = @iStatus2";

                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iStatus1", 0);
                pc.Add("@fMoney", request.amount);
                pc.Add("@iId", request.merchantOrderId);
                pc.Add("@iStatus2", -2);

                result.data = dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = Result.ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("DuitkuProvider::SetDuitkuPaybackRecordStaus", "{0}", ex.Message);
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