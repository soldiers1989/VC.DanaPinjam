using NF.AdminSystem.Models;
using NF.AdminSystem.Providers;
using RedisPools;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using YYLog.ClassLibrary;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using NF.AdminSystem.Models.v2;

namespace NF.AdminSystem.Controllers.v2
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/v2/Main")]
    [ApiController]
    public class MainController : ControllerBase
    {
        private AppSettingsModel ConfigSettings { get; set; }

        public MainController(IOptions<AppSettingsModel> settings)
        {
            ConfigSettings = settings.Value;
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("GetBankCodes")]
        /// <summary>
        /// 获取贷款种类
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetBankCodes()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                Redis redis = HelperProvider.GetRedis();
                string key = "BankCodes";
                string retJson = redis.StringGet(key);

                if (String.IsNullOrEmpty(retJson))
                {
                    DataProviderResultModel result = MainInfoProvider.GetBankCodes();
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
                    redis.StringSet(key, JsonConvert.SerializeObject(ret), 300);
                }
                else
                {
                    return retJson;
                }
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetBankCodes", "异常：{0}", ex.Message);
            }

            return JsonConvert.SerializeObject(ret);
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("GetInitDebitStyle")]
        /// <summary>
        /// 获取贷款种类
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetInitDebitStyle()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            Redis redis = HelperProvider.GetRedis();
            try
            {
                string userId = HttpContext.Request.Headers["userId"];
                var iUserId = 0;
                var userLevel = 0;
                int.TryParse(userId, out iUserId);
                if (iUserId > 0)
                {
                    string key = String.Format("UserAllInfoV5_{0}", userId);
                    string info = redis.StringGet(key);
                    if (!String.IsNullOrEmpty(info))
                    {
                        UserAllInfoModel userInfo = JsonConvert.DeserializeObject<UserAllInfoModel>(info);
                        userLevel = userInfo.userLevel;

                        Log.WriteDebugLog("v2::MainController::GetInitDebitStyle", "用户的等级是：{0}", userLevel);
                    }
                    else
                    {
                        DataProviderResultModel dataProviderResult = UserProvider.GetUserAllInfo(userId);
                        if (dataProviderResult.result == Result.SUCCESS)
                        {
                            UserAllInfoModel userInfo = dataProviderResult.data as UserAllInfoModel;
                            userLevel = userInfo.userLevel;
                            redis.StringSet(key, JsonConvert.SerializeObject(userInfo));
                        }
                        else
                        {
                            Log.WriteDebugLog("v2::MainController::GetInitDebitStyle", "获取缓存与数据库为空：{0}", userId);
                        }
                    }
                }
                else
                {
                    Log.WriteDebugLog("v2::MainController::GetInitDebitStyle", "用户ID没有传入");
                    userLevel = 0;
                }

                var debitStyle = new List<float> { 1500000.00f, 2100000.00f, 2700000.00f };
                var debitDesc = new SortedList<float, string>();
                debitDesc.Add(1500000.00f, "ISI LENGKAP DATA PRIBADI ANDA DENGAN BENAR MAKA SYSTEM CREDIT KITA AKAN MELAKUKAN PENGECEKAN DAN PINJAMAN AKAN DIBERIKAN SECARA AUTOMATIS BILA LOLOS VERIFIKASI. TERIMA KASIH");
                debitDesc.Add(2100000.00f, "PEMINJAMAN DENGAN NOMINAL INI HANYA BISA DIPINJAMKAN KALAU SUDAH PERNAH MELAKUKAN PEMBAYARAN TEPAT WAKTU ATAU PERPANJANGAN PRODUCT A DENGAN NOMINAL RP 1.500.000 SEBANYAK 2 KALI PEMINJAMAN");
                debitDesc.Add(2700000.00f, "PEMINJAMAN DENGAN NOMINAL INI HANYA BISA DIPINJAMKAN KALAU SUDAH PERNAH MELAKUKAN PEMBAYARAN TEPAT WAKTU ATAU PERPANJANGAN PRODUCT B DENGAN NOMINAL RP 2.100.000 SEBANYAK 2 KALI PEMINJAMAN");

                var debitPeriod = new List<int> { 7 };
                List<object> retList = new List<object>();
                foreach (var style in debitStyle)
                {
                    List<DebitInfo> list = new List<DebitInfo>();
                    foreach (var period in debitPeriod)
                    {
                        DebitInfo info = new DebitInfo();
                        info.debitMoney = style;
                        info.debitPeriod = period;

                        info.description = "Ketika Anda melakukan pinjam\r\nBiaya admin harus dibayar diawal";
                        DataProviderResultModel result = DebitProvider.GetInterestRateByDebitStyle(style, period);
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
                                info.payBackMoney = style;
                                //手续费，一次性
                                if (rate >= 1)
                                {
                                    info.debitFee = rate;
                                    //日息
                                    info.dailyInterest = rate / period;
                                }
                                else
                                {
                                    info.debitFee = style * rate;
                                    //日息
                                    info.dailyInterest = style * rate / period;
                                }
                                info.adminFee = String.Format("Biaya Admin Rp {0}", info.debitFee);
                                //实际到帐，减去手续费
                                info.actualMoney = style - info.debitFee;

                                //逾期日息
                                info.overdueDayInterest = info.actualMoney * overdueRate;

                                //描述
                                info.description = debitDesc[style];
                                info.displayStyle = 1;
                                if (userLevel == 0)
                                {
                                    if (style > 1500000)
                                    {
                                        info.displayStyle = 0;
                                    }
                                }
                                if (userLevel == 1)
                                {
                                    if (style > 2100000)
                                    {
                                        info.displayStyle = 0;
                                    }
                                }
                                list.Add(info);
                            }
                        }
                    }
                    retList.Add(new { debitMoney = style, debitCombination = list });
                }

                ret.data = retList;

            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::MainController::GetInitDebitStyle", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("GetInitAppConfig")]
        /// <summary>
        /// 启动时获取全局的配置信息
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetInitAppConfig()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                string version = HttpContext.Request.Headers["version"];
                string pkgName = HttpContext.Request.Headers["pkgName"];
                int iVersion = 0;
                int.TryParse(version, out iVersion);

                Redis redis = HelperProvider.GetRedis();
                version = redis.StringGet("appVersion");
                int newVersion = 0;
                int.TryParse(version, out newVersion);

                int updateIsMust = 0;
                int.TryParse(redis.StringGet("updateIsMust"), out updateIsMust);
                SortedList<string, string> list = new SortedList<string, string>();
                if (String.IsNullOrEmpty(pkgName))
                {
                    list.Add("isUpdate", "1");
                    list.Add("version", Convert.ToString(newVersion));
                    list.Add("isMust", "0");

                    Log.WriteDebugLog("MainController::GetInitAppConfig", "老版本：提示升级，{0}", HelperProvider.GetHeader(HttpContext));
                }
                else
                {
                    list.Add("isUpdate", Convert.ToString(newVersion > iVersion ? 1 : 0));
                    list.Add("version", Convert.ToString(newVersion));
                    list.Add("isMust", Convert.ToString(updateIsMust));
                }
                //Anda masih menggunakan aplikasi versi lama, silahkan klik https://play.google.com/store/apps/details?id=com.danapinjam.vip untuk mengunduh versi terbaru.
                list.Add("downloadUrl", "https://play.google.com/store/apps/details?id=com.danapinjam.vip");
                list.Add("aboutUrl", "http://api.danapinjam.com/api/Home/about");
                list.Add("helpUrl", "http://api.danapinjam.com/api/Home/Help");
                list.Add("contactusUrl", "http://api.danapinjam.com/api/Home/contactus");
                list.Add("agreementUrl2", "http://api.danapinjam.com/api/Home/agreement2");
                list.Add("agreementUrl", "http://api.danapinjam.com/api/Home/agreement");

                ///下面是第三方支付的说明页面
                list.Add("atmh5url", ConfigSettings.atmh5url);
                list.Add("paymethod", String.IsNullOrEmpty(ConfigSettings.PayMethod) ? "1" : ConfigSettings.PayMethod);

                ///以下是OSS的相关配置
                list.Add("ossRegion", "ap-southeast-5");
                list.Add("bucketName", "yjddebit");
                list.Add("ossEndPoint", "http://oss-ap-southeast-5.aliyuncs.com");
                list.Add("ossUrl", "http://yjddebit.oss-ap-southeast-5.aliyuncs.com");

                list.Add("privacyPolicy", "http://api.smalldebit.club/static/html/privacy.html");
                list.Add("indexIntro", "Lakukan Pembayaran Tepat Waktu\r\n&\r\nNikmati Pinjaman Yang Lebih Besar");

                ret.data = list;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::MainController::GetInitAppConfig", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("PublishVersion")]
        public ActionResult<string> PublishVersion(int version, int isMust)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                Redis redis = HelperProvider.GetRedis();
                string result = Convert.ToString(redis.StringSet("appVersion", version));
                result += Convert.ToString(redis.StringSet("updateIsMust", isMust));

                ret.data = result;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetCertificate", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
        [AllowAnonymous]
        [Route("GetSelection")]
        public ActionResult<string> GetSelection()
        {
            List<SelectionModel> list = new List<SelectionModel>();
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ret.data = MainInfoProvider.GetSelection();
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetSelection", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [Route("GetSTSToken")]
        public ActionResult<string> GetSTSToken()
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

                    Log.WriteErrorLog("v2:MainController::GetSTSToken", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }
                var requestBody = JsonConvert.DeserializeObject<STSTokenRequestBody>(content);

                ret.data = HelperProvider.GetToken(requestBody.userId, requestBody.region);
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::MainController::GetSTSToken", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        [Route("GetNotices")]
        public ActionResult<string> GetNotices()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                DataProviderResultModel result = MainInfoProvider.GetNotices();
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
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::MainController::GetNotice", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpGet]
        [HttpPost]
        [Route("GetUserDebitAttention")]
        public ActionResult<string> GetUserDebitAttention()
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

                    Log.WriteErrorLog("v2:MainController::GetUserDebitAttention", "请求参数为空。{0}", HelperProvider.GetHeader(HttpContext));
                    return JsonConvert.SerializeObject(ret);
                }

                var requestBody = JsonConvert.DeserializeObject<UserInfoRequestBody>(content);

                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserDebitAttention(requestBody.userId);
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
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("v2::MainController::GetUserDebitAttention", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }
    }
}
