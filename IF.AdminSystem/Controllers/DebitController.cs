
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

namespace NF.AdminSystem.Controllers
{
    [Route("api/Debit")]
    /// <summary>
    /// 贷款相关的接口
    /// </summary>
    public class DebitController : ControllerBase
    {
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
        public ActionResult<string> CalcInterestRate(float debitMoney, int debitPeriod)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DebitInfo info = new DebitInfo();
                info.debitMoney = debitMoney;
                info.debitPeriod = debitPeriod;
                info.description = String.Format("no interest rate fees\r\nonly pay back {0} after {1} days", debitMoney.ToString("#,##0.00 "), debitPeriod);
                DataProviderResultModel result = DebitProvider.GetInterestRateByDebitStyle(debitMoney, debitPeriod);
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
                        info.payBackMoney = debitMoney;

                        //手续费，一次性
                        if (rate >= 1)
                        {
                            info.debitFee = rate;
                            //日息
                            info.dailyInterest = rate / debitPeriod;
                        }
                        else
                        {
                            info.debitFee = debitMoney * rate;
                            //日息
                            info.dailyInterest = debitMoney * rate / debitPeriod;
                        }
                        //实际到帐，减去手续费
                        info.actualMoney = debitMoney - info.debitFee;
                        //日息
                        info.dailyInterest = debitMoney * rate/debitPeriod;
                        //逾期日息
                        info.overdueDayInterest = debitMoney * overdueRate;
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

                Log.WriteErrorLog("DebitController::CalcInterestRate", "异常：{0}", ex.Message);
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
        public ActionResult<string> SubmitDebitRequest(int userId, float debitMoney, int bankId, string description, int debitPeriod = 0,int debitPeroid = 0)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                debitPeriod = debitPeriod == 0 ? debitPeroid : debitPeriod;
                ///逻辑
                DataProviderResultModel result = DebitProvider.SubmitDebitReuqest(userId, debitMoney, debitPeriod, bankId, description);
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
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("DebitController::SubmitDebitRequest", "异常：{0}", ex.Message);
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

        [HttpPost]
        [HttpGet]
        [Route("SubmitPayBackDebitRequest")]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitId"></param>
        /// <param name="payBackDebitMoney"></param>
        /// <param name="certificateUrl"></param>
        /// <returns></returns>
        public ActionResult<string> SubmitPayBackDebitRequest(int userId, int debitId, float payBackDebitMoney,string certificateUrl = "")
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.PayBackDebitRequest(userId, debitId, payBackDebitMoney, certificateUrl);
                ret.result = result.result;
                if (result.result != Result.SUCCESS)
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

                Log.WriteErrorLog("DebitController::SubmitPayBackDebitRequest", "异常：{0}", ex.Message);
            }
            
            return JsonConvert.SerializeObject(ret);
        }


        [HttpPost]
        [HttpGet]
        [Route("SubmitExtendDebitRequest")]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="debitId"></param>
        /// <param name="payBackDebitMoney"></param>
        /// <param name="certificateUrl"></param>
        /// <returns></returns>
        public ActionResult<string> SubmitExtendDebitRequest(int userId, int debitId, float payBackDebitMoney, string certificateUrl = "")
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.ExtendDebitRequest(userId, debitId, payBackDebitMoney, certificateUrl);
                ret.result = result.result;
                if (result.result != Result.SUCCESS)
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

                Log.WriteErrorLog("DebitController::SubmitExtendDebitRequest", "异常：{0}", ex.Message);
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
