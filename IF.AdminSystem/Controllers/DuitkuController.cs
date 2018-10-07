using DBMonoUtility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YYLog.ClassLibrary;
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;
using Newtonsoft.Json;
using RedisPools;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace NF.AdminSystem.Controllers
{
    [Route("api/Duitku")]
    public class DuitkuController : Controller
    {
        private AppSettingsModel ConfigSettings { get; set; }

        public DuitkuController(IOptions<AppSettingsModel> settings)
        {
            ConfigSettings = settings.Value;
        }

        private string getDuitkuKey(string merchantCode)
        {
            switch (merchantCode)
            {
                case "D0929":
                    return ConfigSettings.duitkuKey;
                case "D1024":
                    return "70b46f25bc3c74e57120444c44b9a399";
                default:
                    return ConfigSettings.duitkuKey;
            }
        }

        private string getPrefixNo(string target)
        {
            switch (target)
            {
                case "B":
                    return "868007";
                case "A":
                default:
                    return ConfigSettings.prefixNo;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("CallbackRequest")]
        public ActionResult<string> CallbackRequest([FromForm] CallbackRequestModel request)
        {
            Redis redis = new Redis();
            string key = String.Format("CallbackRequest_{0}", request.merchantOrderId);
            try
            {
                redis.LockTake(key, 1);
                if (!request.IsEmpty())
                {
                    string signature = String.Empty;
                    string duitkuKey = getDuitkuKey(request.merchantCode);
                    signature = request.merchantCode + request.amount + request.merchantOrderId + duitkuKey;

                    signature = HelperProvider.MD532(signature);
                    DataProviderResultModel result = DuitkuProvider.SaveDuitkuCallbackRecord(request);
                    string guid = Convert.ToString(result.data);
                    ///记录调用日志，最终需要写入数据库
                    Log.WriteLog("DuitkuController::CallbackRequest", "{0} - {1}", result.result, JsonConvert.SerializeObject(request));

                    //验证签名
                    if (signature == request.signature)
                    {
                        Log.WriteDebugLog("DuitkuController::CallbackRequest", "签名验证通过:{0} - 传入为：{1}", signature, request.signature);
                        ///验证通过
                        DataProviderResultModel ret = DuitkuProvider.SetDuitkuPaybackRecordStaus(request);
                        if (ret.result == Result.SUCCESS)
                        {
                            Log.WriteErrorLog("DuitkuController::CallbackRequest", "回调操作成功。");
                            DuitkuProvider.SetDuitkuCallbackRecordStatus(guid, 1);
                            return "Success";
                        }
                        else
                        {
                            Log.WriteErrorLog("DuitkuController::CallbackRequest", "回调操作失败。");
                            DuitkuProvider.SetDuitkuCallbackRecordStatus(guid, -1);

                            return "Error";
                        }
                    }
                    else
                    {
                        Log.WriteDebugLog("DuitkuController::CallbackRequest", "签名验证没有通过:{0} - 传入为：{1}", signature, request.signature);
                        ///签名不通过
                        return "Bad Signature";
                    }
                }
                else
                {
                    Log.WriteDebugLog("DuitkuController::CallbackRequest", "参数异常，缺少必要的参数:{0}", JsonConvert.SerializeObject(request));
                    return "Bad Parameter";
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("DuitkuController::CallbackRequest", "{0}", ex.Message);
            }
            finally
            {
                redis.LockRelease(key, 1);
            }
            return "Error";
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("InquiryRequest")]
        /// <summary>
        /// 编辑用户联系信息接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> InquiryRequest([FromForm] DuitkuInquriyRequestModel request)
        {
            Redis redis = new Redis();
            string key = String.Format("InquiryRequest_{0}", request.vaNo);

            DuitkuInquriyResponseModel response = new DuitkuInquriyResponseModel();
            try
            {
                if (redis.LockTake(key, 1))
                {
                    Log.WriteDebugLog("DuitkuController::InquiryRequest", "param is {0}", JsonConvert.SerializeObject(request));
                    string signature = String.Empty;
                    string duitkuKey = getDuitkuKey(request.merchantCode);
                    signature = request.merchantCode + request.action + request.vaNo + request.session + duitkuKey;
                    signature = HelperProvider.MD532(signature);

                    if (signature != request.signature)
                    {
                        response.statusCode = "01";
                        response.statusMessage = "signature is incorrect.";
                        Log.WriteDebugLog("DuitkuController::InquiryRequest", "签名验证没有通过:{0} - 传入为：{1}", signature, request.signature);
                    }
                    else
                    {
                        response.statusCode = "01";
                        response.statusMessage = "Request is incorrect.";
                        if (request.IsEmpty())
                        {
                            response.statusCode = "01";
                            response.statusMessage = "Request content is empty.";
                            Log.WriteDebugLog("DuitkuController::InquiryRequest", "参数异常，缺少必要的参数:{0}", JsonConvert.SerializeObject(request));
                        }
                        else
                        {
                            /// 868005 = A，868007 = B
                            if (request.bin == "868005" || request.bin == "868007" || request.bin == "119905" || request.bin == "119906")
                            {
                                string vaNo = request.vaNo.Replace(request.bin, "");
                                if (vaNo.Length == (16 - ConfigSettings.prefixNo.Length))
                                {
                                    string type = vaNo.Substring(0, 1);
                                    string debitId = vaNo.Substring(1);
                                    int iDebitId = -1;
                                    request.merchantCode = String.Format("{0}{1}", request.bin, request.merchantCode);
                                    int.TryParse(debitId, out iDebitId);
                                    ///3 － 延期；4 － 还款
                                    if (type == "3")
                                    {
                                        var ret = DebitProvider.GetUserExtendRecord(iDebitId);
                                        if (ret.result == Result.SUCCESS)
                                        {
                                            var model = ret.data as DebitExtendModel;

                                            //状态为未还款，还款失败，逾期才能进行延期
                                            if (model.status == 1 || model.status == -2 || model.status == 4)
                                            {
                                                var result = DuitkuProvider.CreatePayBack(model.userId, iDebitId, type, request.merchantCode);

                                                if (result.result == Result.SUCCESS)
                                                {
                                                    response.statusCode = "00";
                                                    response.statusMessage = "success";
                                                    response.merchantOrderId = Convert.ToString(result.data);
                                                    response.vaNo = request.vaNo;
                                                    response.amount = "0";//Convert.ToString(model.extendFee + model.overdueMoney);
                                                    response.name = "DanaPinjam";
                                                }
                                                else
                                                {
                                                    response.statusCode = "01";
                                                    response.statusMessage = "Create extend record incorrect.";
                                                    Log.WriteErrorLog("DuitkuController::InquiryRequest", "Create extend record incorrect:{0} - {1}", iDebitId, result.message);
                                                }
                                            }
                                            else
                                            {
                                                response.statusCode = "01";
                                                response.statusMessage = "Only within loan, overdue, payback failed , can request extend";
                                                Log.WriteErrorLog("DuitkuController::InquiryRequest", "记录的状态为:{0}，不能申请延期 - {1}", model.status, iDebitId);
                                            }
                                        }
                                        else
                                        {
                                            response.statusCode = "01";
                                            response.statusMessage = ret.message;
                                            Log.WriteErrorLog("DuitkuController::InquiryRequest", "Get extend record incorrect:{0} - {1}", iDebitId, ret.message);
                                        }
                                    }
                                    else if (type == "4")
                                    {
                                        var ret = DebitProvider.GetUserDebitRecord(iDebitId);

                                        if (ret.result == Result.SUCCESS)
                                        {
                                            var model = ret.data as DebitInfoModel;

                                            if (model.status == -2 || model.status == 1 || model.status == 4)
                                            {
                                                var result = DuitkuProvider.CreatePayBack(model.userId, iDebitId, type, request.merchantCode);

                                                if (result.result == Result.SUCCESS)
                                                {
                                                    response.statusCode = "00";
                                                    response.statusMessage = "success";
                                                    response.merchantOrderId = Convert.ToString(result.data);
                                                    response.vaNo = request.vaNo;
                                                    response.amount = "0";//Convert.ToString(model.payBackMoney + model.overdueMoney);
                                                    response.name = "DanaPinjam";
                                                }
                                                else
                                                {
                                                    response.statusCode = "01";
                                                    response.statusMessage = "Create payback record incorrect.";
                                                    Log.WriteErrorLog("DuitkuController::InquiryRequest", "Create payback record incorrect:{0} - {1}", iDebitId, result.message);
                                                }
                                            }
                                            else
                                            {
                                                response.statusCode = "01";
                                                response.statusMessage = "Only within loan, overdue, payback failed , can request payback.";
                                                Log.WriteErrorLog("DuitkuController::InquiryRequest", "记录的状态为:{0}，不能申请延期 - {1}", model.status, iDebitId);
                                            }
                                        }
                                        else
                                        {
                                            response.statusCode = "01";
                                            response.statusMessage = ret.message;
                                            Log.WriteErrorLog("DuitkuController::InquiryRequest", "get debit record incorrect.{0} message = {1}", iDebitId, ret.message);
                                        }
                                    }
                                    else
                                    {
                                        response.statusCode = "01";
                                        Log.WriteErrorLog("DuitkuController::InquiryRequest", "param is incorrect. request.type:{0} debitId = {1}", type, iDebitId);
                                    }
                                }
                                else
                                {
                                    response.statusCode = "01";
                                    response.statusMessage = "va is incorrect.";
                                    Log.WriteErrorLog("DuitkuController::InquiryRequest", "va is incorrect. request.type:{0}", vaNo);
                                }
                            }
                            else
                            {
                                response.statusCode = "01";
                                Log.WriteErrorLog("DuitkuController::InquiryRequest", "param is incorrect. request:{0}", JsonConvert.SerializeObject(request));
                            }
                        }
                    }
                }
                else
                {
                    Log.WriteErrorLog("UserController::InquiryRequest", "get lock fail.{0}", JsonConvert.SerializeObject(request));
                }
            }
            catch (Exception ex)
            {
                response.statusCode = "01";
                response.statusMessage = ex.Message;
                Log.WriteErrorLog("UserController::InquiryRequest", "异常：{0}", ex.Message);
            }
            finally
            {
                redis.LockRelease(key, 1);
            }
            return JsonConvert.SerializeObject(response);
        }

        [Route("GetDuitkuVAInfo")]
        [HttpPost]
        [HttpGet]
        public ActionResult GetDuitkuVAInfo(int userId, int debitId, int type)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///3为还款，4为延期
                if (type != 3 && type != 4)
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "type is Incorrect.";
                }
                else
                {
                    ///这里验证debitId 与 用户
                    string prefix = ConfigSettings.prefixNo;//HelperProvider.PrefixOfDuitku();
                    DataProviderResultModel result = null;

                    result = type == 3 ? DebitProvider.GetUserExtendRecord(debitId) : DebitProvider.GetUserDebitRecord(debitId);

                    if (result.result == Result.SUCCESS)
                    {
                        int dataUserId = 0;
                        if (type == 3)
                        {
                            DebitExtendModel model = result.data as DebitExtendModel;
                            dataUserId = model.userId;
                            prefix = getPrefixNo(model.target);

                            ViewData["money"] = (model.extendFee + model.overdueMoney - model.partMoney).ToString("N0").Replace(",", ".");
                        }
                        else
                        {
                            DebitInfoModel model = result.data as DebitInfoModel;
                            dataUserId = model.userId;
                            prefix = getPrefixNo(model.target);
                            ViewData["money"] = (model.payBackMoney).ToString("N0").Replace(",", ".");
                        }
                        ViewData["prefix"] = prefix;

                        if (userId == dataUserId)
                        {
                            DataProviderResultModel bankInfoResult = UserProvider.GetUserBankInfo(Convert.ToString(userId));

                            if (bankInfoResult.result == Result.SUCCESS)
                            {
                                UserBankInfoModel bankInfo = bankInfoResult.data as UserBankInfoModel;

                                string vaNo = String.Format("{0}{1}{2}", prefix, type, debitId.ToString().PadLeft(15 - prefix.Length, '0'));
                                ViewData["title"] = type == 3 ? "Anda dapat melakukan pembayaran dengan menggunakan" : "Anda dapat melakukan pembayaran dengan menggunakan";
                                ViewData["vaNo"] = vaNo;
                                ViewData["userName"] = bankInfo.contactName;
                            }
                            else
                            {
                                ret.result = Result.ERROR;
                                ret.message = "bank info inquiry is incorrect.";
                            }

                        }
                        else
                        {
                            ret.result = Result.ERROR;
                            ret.message = String.Format("params is incorrect.({0}:{1})", userId, dataUserId);
                        }
                    }
                    else
                    {
                        ret.result = result.result;
                        ret.message = result.message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::InquiryRequest function.";

                Log.WriteErrorLog("UserController::GetDuitkuVAInfo", "异常：{0}", ex.Message);
            }

            if (ret.result == Result.SUCCESS)
            {
                return View();
            }
            else
            {
                ViewData["refresh"] = HttpContext.Request.Path + HttpContext.Request.QueryString;
                ViewData["message"] = ret.message;
                return View("Error");
            }
        }
    }
}