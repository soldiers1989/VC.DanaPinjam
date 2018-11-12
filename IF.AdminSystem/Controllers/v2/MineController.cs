using Newtonsoft.Json;
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers.v2;
using RedisPools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YYLog.ClassLibrary;
using Microsoft.Extensions.Options;
using NF.AdminSystem.Models.v2;


namespace NF.AdminSystem.Controllers.v2
{
    [Route("api/v2/Mine")]
    /// <summary>
    /// 用户相关接口
    /// </summary>
    [ApiController]
    public class MineController : ControllerBase
    {
        [HttpPost]
        [Route("GetUserQuestions")]
        public ActionResult<string> GetUserQuestions()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;

            string content = HelperProvider.GetRequestContent(HttpContext);
            if (String.IsNullOrEmpty(content))
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";

                Log.WriteErrorLog("v2:MineController::GetUserQuestions", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                return JsonConvert.SerializeObject(ret);
            }

            var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);
            try
            {
                DataProviderResultModel result = MineProvider.GetUserQuestions(requestBody);
                if (result.result == Result.SUCCESS)
                {
                    ret.data = result.data;
                    ret.result = Result.SUCCESS;
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
                ret.message = "The program logic error from the MineController::GetUserQuestions function.";

                Log.WriteErrorLog("v2::MineController::GetUserQuestions", "UserInfo:{0}，异常：{1}", content, ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("PostQuestions")]
        public ActionResult<string> PostQuestions()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;

            string content = HelperProvider.GetRequestContent(HttpContext);
            if (String.IsNullOrEmpty(content))
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";

                Log.WriteErrorLog("v2:MineController::PostQuestions", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                return JsonConvert.SerializeObject(ret);
            }

            var requestBody = JsonConvert.DeserializeObject<QuestionsRequestBody>(content);
            try
            {
                DataProviderResultModel result = MineProvider.PostUserQuestions(requestBody);
                if (result.result == Result.SUCCESS)
                {
                    ret.data = result.data;
                    ret.result = Result.SUCCESS;
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
                ret.message = "The program logic error from the MineController::PostQuestions function.";

                Log.WriteErrorLog("v2::MineController::PostQuestions", "UserInfo:{0}，异常：{1}", content, ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }
    }
}