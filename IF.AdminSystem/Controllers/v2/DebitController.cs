﻿
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;
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

        [AllowAnonymous]
        [Route("CalcInterestRate")]
        [HttpPost]
        [HttpGet]
        /// <summary>
        /// 计算利率 2
        /// </summary>
        /// <param name="debitMoney"></param>
        /// <param name="debitPeroid"></param>
        /// <returns></returns>
        public ActionResult<string> CalcInterestRate()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                string content = HelperProvider.GetRequestContent(HttpContext);
                if (String.IsNullOrEmpty(content))
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "The request body is empty.";

                    Log.WriteErrorLog("v2:DebitController::CalcInterestRate", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<InterestRateRequestBody>(content);
                ///逻辑
                DebitInfo info = new DebitInfo();
                info.debitMoney = requestBody.debitMoney;
                info.debitPeriod = requestBody.debitPeriod;
                info.description = String.Format("no interest rate fees\r\nonly pay back {0} after {1} days", requestBody.debitMoney.ToString("#,##0.00 "), requestBody.debitPeriod);
                DataProviderResultModel result = DebitProvider.GetInterestRateByDebitStyle(requestBody.debitMoney, requestBody.debitPeriod);
                if (result.result == Result.SUCCESS)
                {
                    float rate = 0f;
                    float overdueRate = 0f;

                    if (null != result.data)
                    {
                        List<float> rates = result.data as List<float>;

                        rate = rates[0];
                        overdueRate = rates[1];
                        //贷多少，还多少
                        info.payBackMoney = requestBody.debitMoney;

                        //手续费，一次性
                        if (rate >= 1)
                        {
                            info.debitFee = rate;
                            //日息
                            info.dailyInterest = rate / requestBody.debitPeriod;
                        }
                        else
                        {
                            info.debitFee = requestBody.debitMoney * rate;
                            //日息
                            info.dailyInterest = requestBody.debitMoney * rate / requestBody.debitPeriod;
                        }
                        //实际到帐，减去手续费
                        info.actualMoney = requestBody.debitMoney - info.debitFee;
                        //日息
                        info.dailyInterest = requestBody.debitMoney * rate / requestBody.debitPeriod;
                        //逾期日息
                        info.overdueDayInterest = requestBody.debitMoney * overdueRate;
                        ret.data = info;
                    }
                    else
                    {
                        ret.result = MainErrorModels.LOGIC_ERROR;
                        ret.message = "logic error,The rate calc result is 0.";
                    }
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = result.result;
                    ret.message = result.message;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::DebitController::CalcInterestRate", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
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
                        
                        Log.WriteWarning("DebitController::SubmitDebitRequest", "警告：用户【{0}】提交时BankId为空。", requestBody.userId);
                        return JsonConvert.SerializeObject(ret);
                    }

                    ///逻辑
                    DataProviderResultModel result = DebitProvider.SubmitDebitReuqest(requestBody.userId, requestBody.debitMoney, requestBody.debitPeriod, requestBody.bankId, requestBody.description, requestBody.deviceId);
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

                    Log.WriteDebugLog("DebitController::SubmitDebitRequest", "[{0}] 重复请求。", requestBody.userId);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("DebitController::SubmitDebitRequest", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::SubmitDebitRequest", "{0}", HelperProvider.GetHeader(HttpContext));
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
        public ActionResult<string> GetUserDebitRecords(int userId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserDebitRecords(userId);
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

                Log.WriteErrorLog("DebitController::GetUserDebitRecords", "UserId:{0}，异常：{1}", userId, ex.Message);
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
        public ActionResult<string> GetUserDebitRecord(int debitId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserDebitRecord(debitId);
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
                ret.message = "The program logic error from the DebitController::GetUserDebitRecord function.";

                Log.WriteErrorLog("DebitController::GetUserDebitRecord", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }

        [Route("GetUserExtendInfo")]
        [HttpPost]
        [HttpGet]
        /// <summary>
        /// 获取某条贷款记录
        /// </summary>
        /// <param name="debitId"></param>
        /// <returns></returns>
        public ActionResult<string> GetUserExtendInfo(int debitId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserExtendRecord(debitId);
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
                ret.message = "The program logic error from the DebitController::GetUserDebitRecord function.";

                Log.WriteErrorLog("DebitController::GetUserDebitRecord", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="userId"></param>
        /// <param name="certificateType"></param>
        /// <returns></returns>
        [Route("GetUserCertificate")]
        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        public ActionResult<string> GetUserCertificate(int objectId, int userId, int certificateType)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                ret.data = UserProvider.GetUserCertificate(objectId, userId);
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("DebitController::GetUserCertificate", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }
    }
}
