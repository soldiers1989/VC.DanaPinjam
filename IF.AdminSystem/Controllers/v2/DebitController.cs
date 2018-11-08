
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Net;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using YYLog.ClassLibrary;
using RedisPools;
using Microsoft.Extensions.Options;
using NF.AdminSystem.Models.v2;

namespace NF.AdminSystem.Controllers.v2
{
    [Route("api/v2/Debit")]
    /// <summary>
    /// 贷款相关的接口
    /// </summary>
    public class DebitController : ControllerBase
    {
        private AppSettingsModel ConfigSettings { get; set; }

        public DebitController(IOptions<AppSettingsModel> settings)
        {
            ConfigSettings = settings.Value;
        }

        [HttpPost]
        [Route("SubmitDebitRequest")]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitMoney"></param>
        /// <param name="debitPeroid"></param>
        /// <param name="bankId"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public ActionResult<string> SubmitDebitRequest()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                //需要增加银行信息的记录，为后期历史记录做准备。
                string content = HelperProvider.GetRequestContent(HttpContext);
                if (String.IsNullOrEmpty(content))
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "The request body is empty.";

                    Log.WriteErrorLog("v2:DebitController::SubmitDebitRequest", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<SubmitDebitRequestBody>(content);

                string pkgName = HttpContext.Request.Headers["pkgName"];
                string lockKey = "submitdebit";
                if (redis.LockTake(lockKey, requestBody.userId))
                {
                    if (String.IsNullOrEmpty(requestBody.deviceId))
                    {
                        requestBody.deviceId = HttpContext.Request.Headers["deviceNo"];
                    }

                    if (requestBody.bankId == 0)
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "The bankId is empty.";
                        redis.LockRelease(lockKey, requestBody.userId);

                        Log.WriteWarning("v2::DebitController::SubmitDebitRequest", "警告：用户【{0}】提交时BankId为空。", requestBody.userId);
                        return JsonConvert.SerializeObject(ret);
                    }

                    ///逻辑
                    DataProviderResultModel result = DebitProvider.SubmitDebitReuqestV2(requestBody.userId, requestBody.debitMoney, requestBody.debitPeriod, requestBody.bankId, requestBody.description, requestBody.deviceId);
                    ret.result = result.result;
                    if (result.result == Result.SUCCESS)
                    {
                        ret.data = result.data;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                    redis.LockRelease(lockKey, requestBody.userId);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.ALREADY_SUBMIT_REQUEST;
                    ret.message = "already submit request.";

                    Log.WriteDebugLog("v2::DebitController::SubmitDebitRequest", "[{0}] 重复请求。", requestBody.userId);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::DebitController::SubmitDebitRequest", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("v2::DebitController::SubmitDebitRequest", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("GetUserDebitRecords")]
        [HttpPost]
        [HttpGet]
        /// <summary>
        /// 获取用户的贷款记录
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ActionResult<string> GetUserDebitRecords()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;

            string content = HelperProvider.GetRequestContent(HttpContext);
            if (String.IsNullOrEmpty(content))
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";

                Log.WriteErrorLog("v2:DebitController::SubmitDebitRequest", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                return JsonConvert.SerializeObject(ret);
            }
            try
            {
                var requestBody = JsonConvert.DeserializeObject<DebitRecordsRequestBody>(content);

                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserDebitRecords(requestBody);
                if (result.result == Result.SUCCESS)
                {
                    ret.data = result.data;
                }
                else
                {
                    ret.result = result.result;
                    ret.message = result.message;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the DebitController::GetUserDebitRecords function.";

                Log.WriteErrorLog("v2::DebitController::GetUserDebitRecords", "UserId:{0}，异常：{1}", content, ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }

        [Route("GetUserDebitRecord")]
        [HttpPost]
        [HttpGet]
        /// <summary>
        /// 获取某条贷款记录
        /// </summary>
        /// <param name="debitId"></param>
        /// <returns></returns>
        public ActionResult<string> GetUserDebitRecordDetail(int debitId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                DebitRecordLogResponse response = new DebitRecordLogResponse();
                DataProviderResultModel result = DebitProvider.GetUserDebitRecord(debitId);
                if (result.result == Result.SUCCESS)
                {
                    response.debitInfo = result.data as DebitInfoModel;
                    result = DebitProvider.GetUserDebitRecordLogs(debitId);
                    if (result.result == Result.SUCCESS)
                    { 
                        response.logs = result.data as List<DebitRecordLogModel>;
                        ret.data = response;
                        ret.result = Result.SUCCESS;
                    }
                    else
                    {
                        ret.result = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    ret.result = result.result;
                    ret.message = result.message;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the DebitController::GetUserDebitRecordDetail function.";

                Log.WriteErrorLog("v2::DebitController::GetUserDebitRecordDetail", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }
    }
}
