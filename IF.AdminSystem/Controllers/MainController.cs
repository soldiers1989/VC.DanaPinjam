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

namespace NF.AdminSystem.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/Main")]
    [ApiController]
    public class MainController : ControllerBase
    {
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
            try
            {
                ret.data = new
                {
                    debitStyle = new List<float> { 2000000.00f },
                    debitPeriod = new List<int> { 7 }
                };
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetInitDebitStyle", "异常：{0}", ex.Message);
            }
            
            return JsonConvert.SerializeObject(ret);
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("GetInitDebitStyleV2")]
        /// <summary>
        /// 获取贷款种类
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetInitDebitStyleV2()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                var debitStyle = new List<float> { 1500000.00f };
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
                                //实际到帐，减去手续费
                                info.actualMoney = style - info.debitFee;
                                
                                //逾期日息
                                info.overdueDayInterest = style * overdueRate;
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

                Log.WriteErrorLog("MainController::GetInitDebitStyle", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpGet]
        [Route("GetInitSlogen")]
        /// <summary>
        /// 获取启动页的轮播图
        /// </summary>
        /// <returns></returns>
        public ActionResult<string> GetInitSlogen()
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                List<string> list = new List<string>();
                //list.Add("http://storage.baomihua.com/h5/xinliao/images/init.png");

                ret.data = list;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetInitSlogen", "异常：{0}", ex.Message);
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
                int iVersion = 0;
                int.TryParse(version, out iVersion);

                Redis redis = HelperProvider.GetRedis();
                version = redis.StringGet("appVersion");
                int newVersion = 0;
                int.TryParse(version, out newVersion);

                int updateIsMust = 0;
                int.TryParse(redis.StringGet("updateIsMust"), out updateIsMust);
                SortedList<string, string> list = new SortedList<string, string>();
                list.Add("isUpdate", Convert.ToString(newVersion > iVersion ? 1 : 0));
                list.Add("version", Convert.ToString(newVersion));
                list.Add("isMust", Convert.ToString(updateIsMust));

                list.Add("appName", "PINJAM CEPAT");
                list.Add("totalLoan", "3000000");
                list.Add("totalPeople", "1000");
                list.Add("downloadUrl", "http://test.smalldebit.club/");
                list.Add("aboutUrl", "http://api.danapinjam.com/Home/about");
                list.Add("helpUrl", "http://api.danapinjam.com/Home/Help");
                list.Add("contactusUrl", "http://api.danapinjam.com/Home/contactus");
                list.Add("agreementUrl2", "http://api.danapinjam.com/Home/agreement2");
                list.Add("agreementUrl", "http://api.danapinjam.com/Home/agreement");

                ///以下是OSS的相关配置
                list.Add("ossRegion", "ap-southeast-5");
                list.Add("bucketName", "yjddebit");
                list.Add("ossEndPoint", "http://oss-ap-southeast-5.aliyuncs.com");
                list.Add("ossUrl", "http://yjddebit.oss-ap-southeast-5.aliyuncs.com");

                ret.data = list;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetInitAppConfig", "异常：{0}", ex.Message);
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
        [HttpGet]
        [Route("GetCertificate")]
        /// <summary>
        /// 获取用户凭证
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="certType">凭证类型</param>
        /// <param name="objId">相关id</param>
        /// <returns></returns>
        public ActionResult<string> GetCertificate(int userId, int certType, int objId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                List<string> list = new List<string>();
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");

                ret.data = list;
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [HttpGet]
        [AllowAnonymous]
        [Route("GetSTSToken")]
        public ActionResult<string> GetSTSToken(int userId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ret.data = HelperProvider.GetToken(userId);
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetSTSToken", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
        [AllowAnonymous]
        [Route("GetSTSTokenV2")]
        public ActionResult<string> GetSTSTokenV2(int userId, string region)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ret.data = HelperProvider.GetToken(userId, region);
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::GetSTSToken", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpPost]
        [HttpGet]
        [Route("PostCertificate")]
        /// <summary>
        /// 获取用户凭证
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="certType">凭证类型</param>
        /// <param name="objId">相关id</param>
        /// <returns></returns>
        public ActionResult<string> PostCertificate(int userId, int certType, int objId, string url)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                List<string> list = new List<string>();
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");
                list.Add("http://storage.baomihua.com/h5/apptg/timg.jpg");

                ret.data = list;
            }
            catch (Exception ex)
            {
                ret.result = Result.ERROR;
                ret.errorCode = MainErrorModels.LOGIC_ERROR;
                ret.message = Convert.ToString(MainErrorModels.LOGIC_ERROR);

                Log.WriteErrorLog("MainController::PostCertificate", "异常：{0}", ex.Message);
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
                ///逻辑
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

                Log.WriteErrorLog("MainController::GetNotice", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }

        [HttpGet]
        [HttpPost]
        [Route("GetUserDebitAttention")]
        public ActionResult<string> GetUserDebitAttention(int userId)
        {
            HttpResultModel ret = new HttpResultModel();
            ret.result = Result.SUCCESS;
            try
            {
                ///逻辑
                DataProviderResultModel result = DebitProvider.GetUserDebitAttention(userId);
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

                Log.WriteErrorLog("MainController::GetUserDebitAttention", "异常：{0}", ex.Message);
            }
            return JsonConvert.SerializeObject(ret);
        }
    }
}
