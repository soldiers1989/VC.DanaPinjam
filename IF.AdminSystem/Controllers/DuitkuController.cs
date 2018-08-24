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

        [AllowAnonymous]
        [HttpPost]
        [Route("CallbackRequest")]
        public ActionResult<string> CallbackRequest([FromForm] CallbackRequestModel request)
        {
            try
            {
                if (!request.IsEmpty())
                {
                    string signature = request.merchantCode + request.amount + request.merchantOrderId + ConfigSettings.duitkuKey;
                    signature = HelperProvider.MD532(signature);
                    DataProviderResultModel result = DuitkuProvider.SaveDuitkuCallbackRecord(request);
                    ///记录调用日志，最终需要写入数据库
                    Log.WriteLog("DuitkuController::CallbackRequest", "{0} - {1}", result.result, JsonConvert.SerializeObject(request));

                    //验证签名
                    if (signature == request.signature)
                    {
                        ///验证通过
                        DuitkuProvider.SetDuitkuPaybackRecordStaus(request);
                    }
                    else
                    {
                        ///签名不通过
                        return "Bad Signature";
                    }
                    return "Success";
                }
                else
                {
                    return "Bad Parameter";
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("DuitkuController::CallbackRequest", "{0}", ex.Message);
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
            DuitkuInquriyResponseModel response = new DuitkuInquriyResponseModel();
            try
            {
                Log.WriteDebugLog("DuitkuController::InquiryRequest", "param is {0}", JsonConvert.SerializeObject(request));

                string signature = request.merchantCode + request.action + request.vaNo + request.session + ConfigSettings.duitkuKey;
                signature = HelperProvider.MD532(signature);

                if (signature != request.signature)
                {
                    response.statusCode = "01";
                    response.statusMessage = "signature is incorrect.";
                }
                else
                {
                    // $params = $merchantCode . $action . $vaNo . $session . $apiKey;
                    response.statusCode = "01";
                    response.statusMessage = "Request is incorrect.";
                    if (request.IsEmpty())
                    {
                        response.statusCode = "01";
                        response.statusMessage = "Request content is empty.";
                    }
                    else
                    {
                        if (request.bin == "868005" || request.bin == "119905" || request.bin == "119906")
                        {
                            string vaNo = request.vaNo.Replace(request.bin, "");
                            if (vaNo.Length == (16 - HelperProvider.PrefixOfDuitku().Length))
                            {
                                string type = vaNo.Substring(0, 1);
                                string debitId = vaNo.Substring(1);
                                int iDebitId = -1;
                                int.TryParse(debitId, out iDebitId);

                                if (type == "3")
                                {
                                    var ret = DebitProvider.GetUserExtendRecord(iDebitId);
                                    if (ret.result == Result.SUCCESS)
                                    {
                                        var model = ret.data as DebitExtendModel;
                                        var result = DuitkuProvider.CreatePayBack(model.userId, iDebitId, type);

                                        if (result.result == Result.SUCCESS)
                                        {
                                            response.statusCode = "00";
                                            response.statusMessage = "success";
                                            response.merchantOrderId = Convert.ToString(result.data);
                                            response.vaNo = request.vaNo;
                                            response.amount = Convert.ToString(model.extendFee + model.overdueMoney);
                                            response.name = "DanaPinjam";
                                        }
                                        else
                                        {
                                            response.statusCode = "01";
                                            response.statusMessage = "Create extend record incorrect.";
                                            Log.WriteErrorLog("DuitkuController::InquiryRequest", "Create extend record incorrect:{0}", result.message);
                                        }
                                    }
                                    else
                                    {
                                        response.statusCode = "01";
                                        response.statusMessage = ret.message;
                                    }
                                }
                                else if (type == "4")
                                {
                                    var ret = DebitProvider.GetUserDebitRecord(iDebitId);

                                    if (ret.result == Result.SUCCESS)
                                    {
                                        var model = ret.data as DebitInfoModel;
                                        var result = DuitkuProvider.CreatePayBack(model.userId, iDebitId, type);

                                        if (result.result == Result.SUCCESS)
                                        {
                                            response.statusCode = "00";
                                            response.statusMessage = "success";
                                            response.merchantOrderId = Convert.ToString(result.data);
                                            response.vaNo = request.vaNo;
                                            response.amount = Convert.ToString(model.payBackMoney + model.overdueMoney);
                                            response.name = "DanaPinjam";
                                        }
                                        else
                                        {
                                            response.statusCode = "01";
                                            response.statusMessage = "Create payback record incorrect.";
                                            Log.WriteErrorLog("DuitkuController::InquiryRequest", "Create payback record incorrect:{0}", result.message);
                                        }
                                    }
                                    else
                                    {
                                        response.statusCode = "01";
                                        response.statusMessage = ret.message;
                                    }
                                }
                                else
                                {
                                    response.statusCode = "01";
                                    Log.WriteErrorLog("DuitkuController::InquiryRequest", "param is incorrect. request.type:{0}", type);
                                }
                            }
                            else
                            {
                                response.statusCode = "01";
                                response.statusMessage = "va is incorrect.";
                            }
                        }
                        else
                        {
                            response.statusCode = "01";
                            Log.WriteErrorLog("DuitkuController::InquiryRequest", "param is incorrect. request.bin:{0}", request.bin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.statusCode = "01";
                response.statusMessage = ex.Message;
                Log.WriteErrorLog("UserController::InquiryRequest", "异常：{0}", ex.Message);
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
                    string prefix = HelperProvider.PrefixOfDuitku();
                    DataProviderResultModel result = null;

                    result = type == 3 ? DebitProvider.GetUserExtendRecord(debitId) : DebitProvider.GetUserDebitRecord(debitId);

                    if (result.result == Result.SUCCESS)
                    {
                        if (type == 3)
                        {
                            DebitExtendModel model = result.data as DebitExtendModel;
                            ViewData["money"] = (model.extendFee + model.overdueMoney).ToString("N0").Replace(",", ".");
                        }
                        else
                        {
                            DebitInfoModel model = result.data as DebitInfoModel;
                            ViewData["money"] = (model.payBackMoney + model.overdueMoney).ToString("N0").Replace(",", ".");
                        }

                        string vaNo = String.Format("{0}{1}{2}", prefix, type, debitId.ToString().PadLeft(15 - prefix.Length, '0'));
                        ViewData["title"] = type == 3 ? "Extend" : "Payback";
                        ViewData["vaNo"] = vaNo;
                    }
                    else
                    {
                        ret.result = result.result;
                        ret.message = result.message;
                    }



                    return View();
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::InquiryRequest function.";

                Log.WriteErrorLog("UserController::InquiryRequest", "异常：{0}", ex.Message);
            }
            return View();
        }
    }
}