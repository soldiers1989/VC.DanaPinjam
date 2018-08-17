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

namespace NF.AdminSystem.Controllers
{
    public class DuitkuController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        [Route("InquiryRequest")]
        /// <summary>
        /// 编辑用户联系信息接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> InquiryRequest([FromForm] DuitkuInquriyRequestModel request)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                if (request.IsEmpty())
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "Request content is empty.";
                }
                else
                {
                    if (request.bin == "868005" ||request.bin == "119905") 
                    {
                        string vaNo = request.vaNo.Replace(request.bin, "");

                    }
                    else
                    {
                        Log.WriteErrorLog("DuitkuController::InquiryRequest", "param is incorrect. request.bin:{0}", request.bin);
                    }

                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::InquiryRequest function.";

                Log.WriteErrorLog("UserController::InquiryRequest", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("GetDuitkuVAInfo")]
        [HttpPost]
        [HttpGet]
        public ActionResult<string> GetDuitkuVAInfo(int userId,int debitId, int type)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                if (type != 3 && type != 4)
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "type is Incorrect.";
                }
                else
                {
                    //string vaNo = 
                    
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::InquiryRequest function.";

                Log.WriteErrorLog("UserController::InquiryRequest", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }
    }
}