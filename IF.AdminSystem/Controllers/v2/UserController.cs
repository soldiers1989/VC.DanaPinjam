using Newtonsoft.Json;
using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;
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
    [Route("api/v2/User")]
    /// <summary>
    /// 用户相关接口
    /// </summary>
    [ApiController]
    public class UserController : ControllerBase
    {
        private AppSettingsModel ConfigSettings { get; set; }

        public UserController(IOptions<AppSettingsModel> settings)
        {
            ConfigSettings = settings.Value;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("GetVerificateCode")]
        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public ActionResult<string> GetVerificateCode()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.ERROR;
            try
            {
                string content = HelperProvider.GetRequestContent(HttpContext);
                if (!String.IsNullOrEmpty(content))
                {
                    VerificateRequestBody requestBody = new VerificateRequestBody();
                    requestBody = JsonConvert.DeserializeObject<VerificateRequestBody>(content);
                    if (!String.IsNullOrEmpty(requestBody.phone))
                    {
                        string smsType = ConfigSettings.SMSType;
                        int type = 1;
                        int.TryParse(smsType, out type);
                        return getVerificateCode(requestBody.phone, type);
                    }
                }
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "Please input the phone.";
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("UserController::GetVerificateCode", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        public ActionResult<string> getVerificateCode(string phone, int type)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            string lockKey = "lock_" + phone;
            try
            {
                if (redis.LockTake(lockKey, phone))
                {
                    string IpAddress = string.Empty;

                    if (!String.IsNullOrEmpty(redis.StringGet(String.Format("send_{0}", phone))))
                    {
                        ret.result = Result.SUCCESS;
                        ret.message = String.Format("Already send to {0}.", phone);
                        ret.data = new { recordId = redis.StringGet(String.Format("send_{0}", phone)) };

                        Log.WriteDebugLog("UserController::getVerificateCode", "重复调用，可能是恶意。{0} Ip = {1}", phone, IpAddress);

                        return JsonConvert.SerializeObject(ret);
                    }

                    redis.StringSet(String.Format("send_{0}", phone), DateTime.Now.ToString(), 30);
                    Log.WriteDebugLog("UserController::GetVerificateCode", "手机号为：{0}，开始判断是中国还是印尼手机号。Type = {1}", phone, type);
                    if (type == 0)
                    {
                        string returnJson = String.Empty;
                        DateTime beginTime = DateTime.Now;
                        phone = GetPhone(phone);
                        if (phone.IndexOf("8") == 0)
                        {
                            string sendUrl = String.Format("https://api.nexmo.com/verify/json?api_key={0}&api_secret={1}&number=+62" + phone + "&brand=pinjam&next_event_wait=60&pin_expiry=180",
                                ConfigurationManager.AppSettings.Get("SMSApiKey"), ConfigurationManager.AppSettings.Get("SMSApiSecret"));
                            Log.WriteDebugLog("UserController::GetVerificateCode", "手机号为：{0}，为印尼手机号，开始请求接口：{1}", phone, sendUrl);
                            ///发送逻辑
                            returnJson = HttpWebRequestHelper.Get(sendUrl);
                        }
                        else
                        {

                            string sendUrl = String.Format("https://api.nexmo.com/verify/json?api_key={0}&api_secret={1}&number=+86" + phone + "&brand=pinjam&next_event_wait=60&pin_expiry=180",
                                ConfigurationManager.AppSettings.Get("SMSApiKey"), ConfigurationManager.AppSettings.Get("SMSApiSecret"));

                            Log.WriteDebugLog("UserController::GetVerificateCode", "手机号为：{0}，为中国手机号，开始请求接口：{1}", phone, sendUrl);

                            ///发送逻辑
                            returnJson = HttpWebRequestHelper.Get(sendUrl);
                        }

                        Log.WriteDebugLog("UserController::GetVerificateCode", "发送接口返回结果：{0}, 耗时：{1}ms", returnJson, DateTime.Now.Subtract(beginTime).TotalMilliseconds);
                        var smsObj = JsonConvert.DeserializeObject<SMSSendResultModel>(returnJson);
                        if (smsObj.status != 0)
                        {
                            ret.result = Result.ERROR;
                            ret.message = smsObj.error_text;
                        }
                        else
                        {
                            redis.StringSet("code_" + phone, smsObj.request_id, 300);
                            ret.data = new { recordId = smsObj.request_id };
                        }
                    }
                    else
                    {
                        string returnJson = String.Empty;
                        DateTime beginTime = DateTime.Now;
                        phone = GetPhone(phone);
                        string keyPhone = phone;
                        string code = new Random().Next(666666, 999999).ToString();
                        string guid = Guid.NewGuid().ToString();

                        if (phone.IndexOf("8") == 0)
                        {
                            WaveCellSMSSingleSender.Authorization = ConfigSettings.WaveCellSMSAuthorization;
                            WaveCellSMSSingleSender.SubAccountName = ConfigSettings.WaveCellSMSAccountName;
                            WaveCellSMSSingleSender waveCellSMSSender = new WaveCellSMSSingleSender();
                            phone = "+62" + phone;
                            WaveCellSMSResponseModels sendRet = waveCellSMSSender.Send(phone, String.Format("Kode verifikasi Dana pinjam:{0},tolong isikan Dalam wakus 5 menit.", code));
                            Log.WriteDebugLog("UserController::GetVerificateCode", "{0}", JsonConvert.SerializeObject(sendRet));
                            if (null == sendRet || sendRet.status.code != "QUEUED")
                            {
                                ret.result = Result.ERROR;
                                ret.message = sendRet.status.description;
                            }
                            else
                            {
                                redis.StringSet("code_" + keyPhone, guid, 300);
                                ret.data = new { recordId = guid };
                                redis.StringSet(guid, code);
                            }
                        }
                        else
                        {
                            redis.StringSet("code_" + keyPhone, guid, 300);
                            ret.data = new { recordId = guid };
                            redis.StringSet(guid, "12345");
                        }
                    }
                    redis.LockRelease(lockKey, phone);
                }
                else
                {
                    Log.WriteDebugLog("UserController::getVertificateCode", "并发，过滤之");
                    ret.result = Result.SUCCESS;
                    ret.message = String.Format("Already send to {0}.", phone);
                    ret.data = new { recordId = redis.StringGet(String.Format("send_{0}", phone)) };

                    return JsonConvert.SerializeObject(ret);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::getVertificateCode", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::getVertificateCode", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private int confrimVerificateCode(string phone, string recordId, string code)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                Redis redis = HelperProvider.GetRedis();
                string cacheCode = redis.StringGet(recordId);
                if (!String.IsNullOrEmpty(cacheCode))
                {
                    Log.WriteDebugLog("UserController::confrimVerificateCode", "走到这个逻辑，说明调用了新的短信发送，准备开始验证。缓存中的值为：{0}，用户输入的值为：{1}", cacheCode, code);
                    if (cacheCode == code.Trim())
                    {
                        return 0;
                    }
                }
                else
                {
                    string sendUrl = String.Format("https://api.nexmo.com/verify/check/json?api_key={0}&api_secret={1}&request_id={2}&code={3}"
                        , ConfigurationManager.AppSettings.Get("SMSApiKey"), ConfigurationManager.AppSettings.Get("SMSApiSecret"), recordId, code);
                    ///发送逻辑
                    Log.WriteDebugLog("UserController::confrimVerificateCode", "准备调用发送短信API：{0}", sendUrl);

                    string checkJson = HttpWebRequestHelper.Get(sendUrl);
                    var smsObj = JsonConvert.DeserializeObject<SMSSendResultModel>(checkJson);

                    Log.WriteSystemLog("UserController::confrimVerificateCode", "结果返回：{0}", checkJson);
                    return smsObj.status;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetVertificateCode", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetVertificateCode", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return -1;
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        [Route("UserRegister")]
        /// <summary>
        /// 用户注册接口
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <param name="password">密码</param>
        /// <param name="code">验证码</param>
        /// <returns></returns>
        public ActionResult<string> UserRegister()
        {
            HttpResultModel ret = new HttpResultModel();
            DataProviderResultModel result = new DataProviderResultModel();
            ret.result = Result.SUCCESS;

            int regType = 0;
            string qudao = String.Empty;
            Redis redis = HelperProvider.GetRedis();
            qudao = HttpContext.Request.Headers["pkgName"];
            string phone;

            string content = HelperProvider.GetRequestContent(HttpContext);
            if (String.IsNullOrEmpty(content))
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";

                Log.WriteErrorLog("v2:UserController::UserRegister", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                return JsonConvert.SerializeObject(ret);
            }

            try
            {
                RegisterRequestBody requestBody = JsonConvert.DeserializeObject<RegisterRequestBody>(content);
                phone = GetPhone(requestBody.phone);
                string key = "reg_user";
                if (redis.LockTake(key, phone, 10))
                {
                    string recordId = redis.StringGet("code_" + phone);
                    ///验证码
                    if (!String.IsNullOrEmpty(recordId))
                    {
                        redis.KeyDelete("code_" + phone);

                        if (confrimVerificateCode(phone, recordId, requestBody.code) != 0)
                        {
                            Log.WriteDebugLog("UserController::UserRegister", "Verification code error");
                            ret.result = Result.ERROR;
                            ret.errorCode = MainErrorModels.VERIFICATION_CODE_ERROR;
                            ret.message = "Verification code error";
                            redis.LockRelease(key, phone);
                            return JsonConvert.SerializeObject(ret);
                        }
                        else
                        {
                            Log.WriteDebugLog("UserController::UserRegister", "Verification code Ok");
                            string userName = phone;
                            ///逻辑
                            result = UserProvider.UserRegister(userName, phone, requestBody.password, regType, qudao);

                            if (result.result > 0)
                            {
                                ret.result = Result.SUCCESS;
                                ret.errorCode = 0;
                                ret.message = result.message;

                                UserInfoModel userInfo = result.data as UserInfoModel;
                                string guid = Guid.NewGuid().ToString();
                                redis.StringSet(String.Format("user_guid_{0}", userInfo.userId), guid);
                                userInfo.token = HelperProvider.MD5Encrypt32(String.Format("{0}{1}", userInfo.userId, guid));
                                redis.StringSet(String.Format("UserInfo_{0}", userInfo.userId), JsonConvert.SerializeObject(userInfo));
                                ret.data = userInfo;
                            }
                            else
                            {
                                Log.WriteDebugLog("UserController::UserRegister", "注册失败[{0}]", phone);
                                ret.result = Result.ERROR;
                                ret.errorCode = result.result;
                                ret.message = result.message;
                            }
                        }
                    }
                    else
                    {
                        Log.WriteDebugLog("UserController::UserRegister", "recordId 为空");
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.VERIFICATION_CODE_ERROR;
                        ret.message = "Verification code error";
                        redis.LockRelease(key, phone);
                        return JsonConvert.SerializeObject(ret);
                    }

                    redis.LockRelease(key, phone);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.ALREADY_SUBMIT_REQUEST;
                    ret.message = "already submit request.";
                    Log.WriteDebugLog("UserController::UserRegister", "already submit request.");
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::UserRegister", "异常：{0}，请求参数：{1}", ex.Message, content);
            }
            finally
            {
                Log.WriteDebugLog("UserController::UserRegister", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("ClearUserInfo")]
        public ActionResult<long> ClearUserInfo(int userId)
        {
            Redis redis = HelperProvider.GetRedis();
            string key = String.Format("UserAllInfoV5_{0}", userId);
            long result = redis.KeyDelete(key);

            return result;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("SyncUserRegistration")]
        public ActionResult<string> SyncUserRegistration()
        {
            DataProviderResultModel result = new DataProviderResultModel();
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;

            string content = HelperProvider.GetRequestContent(HttpContext);
            if (String.IsNullOrEmpty(content))
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";

                Log.WriteErrorLog("v2:UserController::SyncUserRegistration", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                return JsonConvert.SerializeObject(ret);
            }

            try
            {
                UserRegistrationRequestBody requestBody = JsonConvert.DeserializeObject<UserRegistrationRequestBody>(content);
                Redis redis = HelperProvider.GetRedis();

                result = UserProvider.SyncUserRegistration(requestBody.userId, requestBody.registrationId);
                string key = String.Format("registrationId_{0}", requestBody.userId);
                redis.StringSet(key, requestBody.registrationId);
                if (result.result != Result.SUCCESS)
                {
                    ret.result = result.result;
                    ret.message = result.message;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::SyncUserRegistration", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::SyncUserRegistration", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("SyncUserThirdPartyInfo")]
        public ActionResult<string> SyncUserThirdPartyInfo()
        {
            DataProviderResultModel result = new DataProviderResultModel();
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                string userId = HttpContext.Request.Headers["userId"];
                string content = HelperProvider.GetRequestContent(HttpContext);
                if (String.IsNullOrEmpty(content))
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "Request body content is empty.";
                    return JsonConvert.SerializeObject(ret);
                }
                else
                {
                    if (content.Length < 10)
                    {
                        ret.result = Result.SUCCESS;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "Request body object is empty.";
                        return JsonConvert.SerializeObject(ret);
                    }
                    FaseBookUserInfo fbUserInfo = JsonConvert.DeserializeObject<FaseBookUserInfo>(content);
                    result = UserProvider.SyncFaceBookUserInfo(userId, fbUserInfo);

                    if (result.result != Result.SUCCESS)
                    {
                        ret.result = result.result;
                        ret.message = result.message;
                    }

                    Redis redis = HelperProvider.GetRedis();
                    string key = String.Format("UserAllInfoV5_{0}", fbUserInfo.userId);
                    redis.KeyDelete(key);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::SyncUserThirdPartyInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::SyncUserThirdPartyInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("UserLogin")]
        /// <summary>
        /// 用户登录接口
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="code">验证码</param>
        /// <param name="loginType">登录类型</param>
        /// <returns></returns>
        public ActionResult<string> UserLogin()
        {
            HttpResultModel ret = new HttpResultModel();
            DataProviderResultModel result = new DataProviderResultModel();
            ret.result = Result.SUCCESS;
            UserLoginRequestBody requestBody = new UserLoginRequestBody();
            try
            {
                string qudao = HttpContext.Request.Headers["pkgName"];

                string content = HelperProvider.GetRequestContent(HttpContext);
                if (String.IsNullOrEmpty(content))
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "The request body is empty.";

                    Log.WriteErrorLog("v2:UserController::UserLogin", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                requestBody = JsonConvert.DeserializeObject<UserLoginRequestBody>(content);

                ///逻辑
                requestBody.phone = GetPhone(requestBody.phone);
                result = UserProvider.UserLogin(requestBody.phone, requestBody.password, qudao, requestBody.loginType);
                if (result.result > 0)
                {
                    Redis redis = HelperProvider.GetRedis();
                    UserInfoModel userInfo = result.data as UserInfoModel;
                    string guid = Guid.NewGuid().ToString();
                    redis.StringSet(String.Format("user_guid_{0}", userInfo.userId), guid);
                    userInfo.token = HelperProvider.MD5Encrypt32(String.Format("{0}{1}", userInfo.userId, guid));
                    redis.StringSet(String.Format("UserInfo_{0}", userInfo.userId), JsonConvert.SerializeObject(userInfo));
                    ret.data = userInfo;

                    string key = String.Format("UserAllInfoV5_{0}", userInfo.userId);

                    if (!String.IsNullOrEmpty(key))
                    {
                        redis.KeyDelete(key);
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
                ret.message = "The program logic error.";

                Log.WriteErrorLog("UserController::UserLogin", "[{0}] 异常:{1}", JsonConvert.SerializeObject(requestBody), ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::UserLogin", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        public static string GetPhone(string phone)
        {
            phone = phone.Trim();
            if (phone.IndexOf("+") == 0)
            {
                phone = phone.Substring(1);
            }
            if (phone.IndexOf("0") == 0)
            {
                phone = phone.Substring(1);
            }
            if (phone.IndexOf("62") == 0)
            {
                phone = phone.Substring(2);
            }
            return phone;
        }

        [HttpGet]
        [AllowAnonymous]
        [HttpPost]
        [Route("UserLogout")]
        /// <summary>
        /// 用户退出接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> UserLogout()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                HttpContext.Session.Clear();
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::UserLogout", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::UserLogout", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }


        /// <summary>
        /// 获取所有用户信息接口，在贷款前验证资料完整性
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("GetUserInfoPercent")]
        public ActionResult<string> GetUserInfoPercent()
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

                    Log.WriteErrorLog("v2:UserController::GetUserInfoPercent", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                UserInfoPercentModel percentModel = new UserInfoPercentModel();
                percentModel.allPercent = userInfo.allPercent;
                percentModel.cardPercent = userInfo.cardPercent;
                percentModel.contactPercent = userInfo.contactPercent;
                percentModel.otherInfoPercent = userInfo.otherInfoPercent;
                percentModel.personalPercent = userInfo.personalPercent;
                percentModel.workingPercent = userInfo.workingPercent;

                ret.data = percentModel;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserInfoPercent", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserInfoPercent", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("GetUserContactInfo")]
        public ActionResult<string> GetUserContactInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserAllInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo.userContactInfo;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserContactInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserContactInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("GetUserWorkingInfo")]
        public ActionResult<string> GetUserWorkingInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserWorkingInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo.userWorkingInfo;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserWorkingInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserWorkingInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }


        [HttpPost]
        [Route("GetUserCardInfo")]
        public ActionResult<string> GetUserCardInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserCardInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo.userCards;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserCardInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserCardInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("GetUserOtherInfo")]
        public ActionResult<string> GetUserOtherInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserOtherInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo.otherInfo;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserOtherInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserOtherInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("GetUserPersonalInfo")]
        public ActionResult<string> GetUserPersonalInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserPersonalInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo.userPersonalInfo;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserPersonalInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserPersonalInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        /// <summary>
        /// 获取所有用户信息接口，在贷款前验证资料完整性
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("GetUserAllInfo")]
        public ActionResult<string> GetUserAllInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserAllInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                UserAllInfoModel userInfo = new UserAllInfoModel();

                Redis redis = HelperProvider.GetRedis();
                string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                string info = redis.StringGet(key);
                if (String.IsNullOrEmpty(info))
                {
                    DataProviderResultModel result = UserProvider.GetUserAllInfo(requestBody.userId);
                    if (result.result == Result.SUCCESS)
                    {
                        redis.StringSet(key, JsonConvert.SerializeObject(result.data));
                        userInfo = result.data as UserAllInfoModel;
                    }
                    else
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = result.result;
                        ret.message = result.message;
                    }
                }
                else
                {
                    userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                }

                ret.data = userInfo;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserAllInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserAllInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("EditUserPersonalInfo")]
        /// <summary>
        /// 编辑用户基本信息接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> EditUserPersonalInfo()
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

                    Log.WriteErrorLog("v2:UserController::EditUserPersonalInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var userInfo = JsonConvert.DeserializeObject<UserPersonalInfoModel>(content);

                DataProviderResultModel checkResult = UserProvider.CheckStatusBeforModifyInfo(userInfo.userId);
                if (checkResult.result != Result.SUCCESS)
                {
                    Log.WriteDebugLog("UserController::EditUserPersonalInfo", "检查到用户【{0}】存在已提交或未还款记录，不允许修改资料。", userInfo.userId);
                    ret.result = checkResult.result;
                    ret.message = checkResult.message;
                    return JsonConvert.SerializeObject(ret);
                }

                DataProviderResultModel result = UserProvider.SaveUserPersonalInfo(userInfo);
                if (result.result != Result.SUCCESS)
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = result.result;
                    ret.message = result.message;
                }
                else
                {
                    Redis redis = HelperProvider.GetRedis();
                    string key = String.Format("UserAllInfoV5_{0}", userInfo.userId);
                    redis.KeyDelete(key);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::EditUserPersonalInfo function.";

                Log.WriteErrorLog("UserController::EditUserPersonalInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::EditUserPersonalInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
        [Route("EditUserWorkingInfo")]
        /// <summary>
        /// 编辑用户工作信息接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> EditUserWorkingInfo()
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

                    Log.WriteErrorLog("v2:UserController::EditUserWorkingInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var workingInfo = JsonConvert.DeserializeObject<UserWorkingInfoModel>(content);

                DataProviderResultModel checkResult = UserProvider.CheckStatusBeforModifyInfo(workingInfo.userId);
                if (checkResult.result != Result.SUCCESS)
                {
                    Log.WriteDebugLog("UserController::EditUserPersonalInfo", "检查到用户【{0}】存在已提交或未还款记录，不允许修改资料。", workingInfo.userId);
                    ret.result = checkResult.result;
                    ret.message = checkResult.message;
                    return JsonConvert.SerializeObject(ret);
                }

                ///逻辑
                DataProviderResultModel result = UserProvider.SaveUserWorkingInfo(workingInfo);
                if (result.result != Result.SUCCESS)
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = result.result;
                    ret.message = result.message;
                }
                else
                {
                    Redis redis = HelperProvider.GetRedis();
                    string key = String.Format("UserAllInfoV5_{0}", workingInfo.userId);
                    redis.KeyDelete(key);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::EditUserWorkingInfo function.";

                Log.WriteErrorLog("UserController::EditUserWorkingInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::EditUserWorkingInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("EditUserContactInfo")]
        /// <summary>
        /// 编辑用户联系信息接口
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> EditUserContactInfo()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                string lockKey = "editUserContact";
                string hUserId = HttpContext.Request.Headers["userId"];
                if (redis.LockTake(lockKey, hUserId, 10))
                {
                    string content = HelperProvider.GetRequestContent(HttpContext);

                    if (String.IsNullOrEmpty(content))
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "Request content is empty.";

                        Log.WriteDebugLog("UserController::EditUserContactInfoV2", "{0}Request content is empty.", hUserId);
                    }
                    else
                    {
                        List<UserContactInfoModel> list = null;

                        if (content.IndexOf("\"data\":") > 0)
                        {
                            RequestBodyModel body = JsonConvert.DeserializeObject<RequestBodyModel>(content);
                            list = JsonConvert.DeserializeObject<List<UserContactInfoModel>>(Convert.ToString(body.data));
                        }
                        else
                        {
                            list = JsonConvert.DeserializeObject<List<UserContactInfoModel>>(content);
                        }

                        int userId = 0;
                        bool isSync = false;
                        foreach (var contactInfo in list)
                        {
                            contactInfo.userId = Convert.ToInt32(hUserId);
                            if (contactInfo.userId > 0)
                            {
                                if (!isSync)
                                {
                                    DataProviderResultModel checkResult = UserProvider.CheckStatusBeforModifyInfo(contactInfo.userId);
                                    if (checkResult.result != Result.SUCCESS)
                                    {
                                        Log.WriteDebugLog("UserController::EditUserContactInfoV2", "检查到用户【{0}】存在已提交或未还款记录，不允许修改资料。", contactInfo.userId);
                                        ret.result = checkResult.result;
                                        ret.message = checkResult.message;

                                        redis.LockRelease(lockKey, hUserId);
                                        return JsonConvert.SerializeObject(ret);
                                    }
                                    isSync = true;
                                }
                                ///逻辑
                                DataProviderResultModel result = UserProvider.SaveUserContactInfo(contactInfo);
                                if (result.result == Result.SUCCESS)
                                {
                                    userId = contactInfo.userId;

                                    Log.WriteDebugLog("UserController::EditUserContactInfoV2", "{0} 修改成功.", hUserId);

                                }
                            }
                            else
                            {
                                Log.WriteDebugLog("UserController::EditUserContactInfoV2", "{0} contactInfo.userId is empty.", hUserId);
                            }
                        }

                        if (userId > 0)
                        {
                            string key = String.Format("UserAllInfoV5_{0}", userId);
                            redis.KeyDelete(key);

                            /*
                            DataProviderResultModel result2 = UserProvider.CheckUserConactsInfo(userId);
                            ret.data = result2.data;
                            ret.result = result2.result;
                            */
                        }
                    }
                    redis.LockRelease(lockKey, hUserId);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.ALREADY_SUBMIT_REQUEST;
                    ret.message = "already submit request.";

                    Log.WriteDebugLog("UserController::EditUserContactInfoV2", "{0} 获取修改锁失败.", hUserId);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::EditUserContactInfo function.";

                Log.WriteErrorLog("UserController::EditUserContactInfoV2", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::EditUserContactInfoV2", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }


        [HttpPost]
        [HttpGet]
        [Route("GetUserBankInfo")]
        /// <summary>
        /// 获取用户的银行卡信息
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetUserBankInfo()
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

                    Log.WriteErrorLog("v2:UserController::GetUserBankInfo", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var userInfo = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                DataProviderResultModel result = UserProvider.GetUserBankInfo(userInfo.userId);
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
                ret.message = "The program logic error from the UserController::GetUserBankInfo function.";
                Log.WriteErrorLog("UserController::GetUserBankInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserBankInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
        [Route("SaveUserBankInfo")]
        public ActionResult<string> SaveUserBankInfo()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                string content = HelperProvider.GetRequestContent(HttpContext);
                string lockKey = "SaveUserBankInfoV2";

                if (!String.IsNullOrEmpty(content))
                {
                    UserBankInfoModel bankInfo = JsonConvert.DeserializeObject<UserBankInfoModel>(content);
                    if (null != bankInfo)
                    {
                        if (redis.LockTake(lockKey, bankInfo.userId, 10))
                        {
                            string version = HttpContext.Request.Headers["version"];
                            int iVersion = 0;
                            int.TryParse(version, out iVersion);

                            if (String.IsNullOrEmpty(bankInfo.bniBankCode) && iVersion > 47)
                            {
                                ret.result = Result.ERROR;
                                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                                ret.message = "Please choose Bank Name.";
                            }
                            else
                            {
                                ///逻辑
                                DataProviderResultModel result = UserProvider.SaveUserBankInfoV2(bankInfo);
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
                            redis.LockRelease(lockKey, bankInfo.userId);
                            return JsonConvert.SerializeObject(ret);
                        }
                        else
                        {
                            ret.result = Result.ERROR;
                            ret.errorCode = MainErrorModels.LOGIC_ERROR;
                            ret.message = "Already request.";
                            return JsonConvert.SerializeObject(ret);
                        }
                    }
                }

                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                ret.message = "The request body is empty.";
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::SaveUserBankInfoV2 function.";
                Log.WriteErrorLog("UserController::SaveUserBankInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::SaveUserBankInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("GetUserPhotos")]
        [HttpPost]
        public ActionResult<string> GetUserIdCardPhotos()
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

                    Log.WriteErrorLog("v2:UserController::GetUserIdCardPhotos", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var userInfo = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);
                int userId = 0;
                if (int.TryParse(userInfo.userId, out userId))
                {
                    ///逻辑
                    ret.data = UserProvider.GetUserCertificate(userId, userId);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "The params is faill.";
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("UserController::GetUserIdCardPhotos", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::GetUserIdCardPhotos", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        [Route("EditUserPhotos")]
        public ActionResult<string> EditUserPhotos()
        {
            HttpResultModel ret = new HttpResultModel();
            Redis redis = HelperProvider.GetRedis();
            ret.result = Result.SUCCESS;
            string lockKey = "EditUserPhotos";
            try
            {
                string content = HelperProvider.GetRequestContent(HttpContext);
                if (String.IsNullOrEmpty(content))
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                    ret.message = "The request body is empty.";

                    Log.WriteErrorLog("v2:UserController::EditUserPhotos", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var requestBody = JsonConvert.DeserializeObject<UserPhotosRequestBody>(content);

                if (redis.LockTake(lockKey, requestBody.userId))
                {
                    if (String.IsNullOrEmpty(requestBody.url))
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "The photo is empty.";
                        return JsonConvert.SerializeObject(ret);
                    }
                    DataProviderResultModel result = UserProvider.UploadUserCertficate(requestBody.userId, requestBody.url, requestBody.type);
                    ret.result = result.result;
                    if (result.result != Result.SUCCESS)
                    {
                        Log.WriteDebugLog("UserController::EditUserPhotos", "保存失败，原因：{0}", result.message);
                        ret.message = result.message;
                    }
                    else
                    {
                        Log.WriteDebugLog("UserController::EditUserPhotos", "保存成功，清除用户缓存。");
                        ret.message = "success";

                        string key = String.Format("UserAllInfoV5_{0}", requestBody.userId);
                        long lret = redis.KeyDelete(key);
                        Log.WriteDebugLog("UserController::EditUserPhotos", "清除用户缓存。({0})", lret);
                    }
                    redis.LockRelease(lockKey, requestBody.userId);
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::UploadUserCertificate function.";

                Log.WriteErrorLog("UserController::UploadUserCertificate", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::UploadUserCertificate", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("PostUserCallRecord")]
        [HttpPost]
        public ActionResult<string> PostUserCallRecord(int userId)
        {

            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                string lockKey = "postUserCall";
                if (redis.LockTake(lockKey, userId, 10))
                {
                    if (null == HttpContext.Request.Body)
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "post request body is null";
                    }
                    else
                    {
                        StreamReader read = new StreamReader(HttpContext.Request.Body);
                        string content = read.ReadToEnd();
                        List<CallRecord> record = null;
                        if (content.IndexOf("\"data\": ") > 0)
                        {
                            RequestBodyModel body = JsonConvert.DeserializeObject<RequestBodyModel>(content);

                            record = JsonConvert.DeserializeObject<List<CallRecord>>(Convert.ToString(body.data));
                        }
                        else
                        {
                            record = JsonConvert.DeserializeObject<List<CallRecord>>(content);
                        }
                        var result = new DataProviderResultModel();
                        var beginTime = DateTime.Now;

                        result = UserProvider.UploadUserConacts(userId, 2, record);

                        result = UserProvider.UpdateUserConactNumber(userId);

                        ret.result = Result.SUCCESS;
                        ret.data = result.data;

                        string key = String.Format("UserAllInfoV5_{0}", userId);
                        redis.KeyDelete(key);

                        Log.WriteDebugLog("UserController::PostUserCallRecord", "{0} use time:{1} ms", content.Length, DateTime.Now.Subtract(beginTime).TotalMilliseconds);
                    }
                    redis.LockRelease(lockKey, userId);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.ALREADY_SUBMIT_REQUEST;
                    ret.message = "already submit request.";
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::PostUserCallRecord function.";

                Log.WriteErrorLog("UserController::PostUserCallRecord", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::PostUserCallRecord", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("PostUserContacts")]
        [HttpPost]
        public ActionResult<string> PostUserContacts(int userId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                string lockKey = "PostUserContacts";
                if (redis.LockTake(lockKey, userId, 10))
                {
                    if (null == HttpContext.Request.Body)
                    {
                        ret.result = Result.ERROR;
                        ret.errorCode = MainErrorModels.PARAMETER_ERROR;
                        ret.message = "post request body is null";
                    }
                    else
                    {
                        StreamReader read = new StreamReader(HttpContext.Request.Body);
                        string content = read.ReadToEnd();
                        List<CallRecord> record = null;
                        if (content.IndexOf("\"data\": ") > 0)
                        {
                            RequestBodyModel body = JsonConvert.DeserializeObject<RequestBodyModel>(content);

                            record = JsonConvert.DeserializeObject<List<CallRecord>>(Convert.ToString(body.data));
                        }
                        else
                        {
                            record = JsonConvert.DeserializeObject<List<CallRecord>>(content);
                        }

                        var result = new DataProviderResultModel();

                        result = UserProvider.UploadUserConacts(userId, 1, record);

                        result = UserProvider.UpdateUserConactNumber(userId);
                        ret.result = Result.SUCCESS;
                        ret.data = result.data;

                        string key = String.Format("UserAllInfoV5_{0}", userId);
                        redis.KeyDelete(key);

                        Log.WriteDebugLog("UserController::PostUserContacts", "{0}", record.Count);
                    }
                    redis.LockRelease(lockKey, userId);
                }
                else
                {
                    ret.result = Result.ERROR;
                    ret.errorCode = MainErrorModels.ALREADY_SUBMIT_REQUEST;
                    ret.message = "already submit request.";
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::PostUserContacts function.";

                Log.WriteErrorLog("UserController::PostUserContacts", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::PostUserContacts", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        [Route("CheckUserConactsInfo")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult<string> CheckUserConactsInfo()
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
                    ret.message = "Request body content is empty.";
                    return JsonConvert.SerializeObject(ret);
                }
                else
                {
                    CheckUserConactsRequestBodyModel requestBody = null;
                    requestBody = JsonConvert.DeserializeObject<CheckUserConactsRequestBodyModel>(content);
                    requestBody.phone = GetPhone(requestBody.phone);
                    DataProviderResultModel result = UserProvider.CheckUserConactsInfo(requestBody);

                    ret.result = result.result;
                    ret.data = result.data;
                    ret.message = result.message;

                    Log.WriteDebugLog("UserController::CheckUserConactsInfo", "Return json is {0}", JsonConvert.SerializeObject(ret));
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::CheckUserConactsInfo function.";

                Log.WriteErrorLog("UserController::CheckUserConactsInfo", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::CheckUserConactsInfo", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("PostUserLocation")]
        [HttpPost]
        public ActionResult<string> PostUserLocation()
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
                    ret.message = "Request body content is empty.";
                    return JsonConvert.SerializeObject(ret);
                }
                else
                {
                    UserLocationModel location = null;
                    if (content.IndexOf("\"data\": ") > 0)
                    {
                        RequestBodyModel body = JsonConvert.DeserializeObject<RequestBodyModel>(content);

                        location = JsonConvert.DeserializeObject<UserLocationModel>(Convert.ToString(body.data));
                    }
                    else
                    {
                        location = JsonConvert.DeserializeObject<UserLocationModel>(content);
                    }

                    DataProviderResultModel result = UserProvider.UpdateUserLocation(location);

                    ret.result = result.result;
                    ret.message = result.message;

                    ret.data = UserProvider.UpdateUserConactNumber(location.userId).data;
                    Redis redis = HelperProvider.GetRedis();
                    string key = String.Format("UserAllInfoV5_{0}", location.userId);
                    redis.KeyDelete(key);

                    Log.WriteDebugLog("UserController::PostUserLocation", "Return json is {0}", JsonConvert.SerializeObject(ret));
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = "The program logic error from the UserController::PostUserLocation function.";

                Log.WriteErrorLog("UserController::PostUserLocation", "异常：{0}", ex.Message);
            }
            finally
            {
                Log.WriteDebugLog("UserController::PostUserLocation", "{0}", HelperProvider.GetHeader(HttpContext));
            }
            return JsonConvert.SerializeObject(ret);
        }
    }
}
