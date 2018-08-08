using Aliyun.Acs.Core.Exceptions;
using Aliyun.Acs.Sts.Model.V20150401;
using Aliyun.Acs.Core.Http;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.Core;

using RedisPools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using YYLog.ClassLibrary;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Configuration;

namespace test
{
    [Serializable]
    public class StsTokenModel
    {
        public int status { get; set; }

        public string AccessKeyId { get; set; }

        public string AccessKeySecret { get; set; }

        public string Security { get; set; }

        public string Expiration { get; set; }
    }
    
    public class HelperProvider
    {
        private const string REGION_CN_HANGZHOU = "cn-hongkong";
        private const string STS_API_VERSION = "2015-04-01";
        private const string AccessKeyID = "LTAITFjGdTJ4M8GX";
        private const string AccessKeySecret = "zPn8XyqIvjhYW37uHIMsDlhJXOUsSU";
        private const string RoleArn = "acs:ram::1878995037257006:role/oss-role";
        private const int TokenExpireTime = 3600;

        private const string PolicyFile = "";
        //这里是权限配置，请参考oss的文档
        
        /*
        private const string PolicyFile = @"{
          ""Statement"": [
            {
              ""Action"": [
                ""oss:PutObject""
              ],
              ""Effect"": ""Allow"",
              ""Resource"": [""acs:oss:*:*:bucketName/*"", ""acs:oss:*:*:bucketName""]
            }
          ],
          ""Version"": ""1""
        }";
         */
        public static string SHA256(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256Managed.Create().ComputeHash(bytes);
            
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }
            return builder.ToString();
        }

        public static string GetRequestContent(HttpContext context)
        {
            if (null == context.Request.Body)
            {
                return String.Empty;
            }
            else
            {
                StreamReader read = new StreamReader(context.Request.Body);
                string content = read.ReadToEnd();
                
                Log.WriteDebugLog("UserController::GetRequestContent", "{0}", content);
                return content;
            }
        }

        public static string SendCertificate(string phone, string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("HelperProvider::SendCertificate", "验证码发送失败：{0}|{1},异常：{2}", phone, code, ex.Message);
                return ex.Message;
            }
            return String.Empty;
        }

        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string MD5Encrypt32(string password)
        {
            string cl = password;
            string pwd = "";
            MD5 md5 = MD5.Create(); //实例化一个md5对像
                                    // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                pwd = pwd + s[i].ToString("X");
            }
            return pwd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string GetRamdomFlag(int n)
        {
            string str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
            StringBuilder SB = new StringBuilder();
            Random rd = new Random();
            for (int i = 0; i < n; i++)
            {
                SB.Append(str.Substring(rd.Next(0, str.Length), 1));
            }
            return SB.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Redis GetRedis()
        {
            Redis redis = new Redis();

            string dbname = ConfigurationManager.AppSettings.Get("DBName");
            //redis.DbIndex = dbname.IndexOf("test") > -1 ? 4 : 3;
            return redis;
        }



        private static AssumeRoleResponse assumeRole(String accessKeyId, String accessKeySecret, String roleArn,
            String roleSessionName, String policy, ProtocolType protocolType, long durationSeconds, string region)
        {
          //  try
          //  {
                // 创建一个 Aliyun Acs Client, 用于发起 OpenAPI 请求
                IClientProfile profile = DefaultProfile.GetProfile(region, accessKeyId, accessKeySecret);
                DefaultAcsClient client = new DefaultAcsClient(profile);

                // 创建一个 AssumeRoleRequest 并设置请求参数
                AssumeRoleRequest request = new AssumeRoleRequest();
                request.Encoding = "utf-8";
                //request.Version = STS_API_VERSION;
                request.Method = MethodType.POST;
                //request.Protocol = protocolType;

                request.RoleArn = roleArn;
                request.RoleSessionName = roleSessionName;
                //request.Policy = policy;
                request.DurationSeconds = durationSeconds;

                // 发起请求，并得到response
                AssumeRoleResponse response = client.GetAcsResponse(request);

                return response;
            //}
            //catch (ClientException e)
            //{
            //   Log.WriteErrorLog("HelperProvider::assumeRole", e.Message);
            //    throw e;
            //}
        }

        public static StsTokenModel GetToken(int userId)
        {
            // 只有 RAM用户（子账号）才能调用 AssumeRole 接口
            // 阿里云主账号的AccessKeys不能用于发起AssumeRole请求
            // 请首先在RAM控制台创建一个RAM用户，并为这个用户创建AccessKeys

            // RoleArn 需要在 RAM 控制台上获取
            // RoleSessionName 是临时Token的会话名称，自己指定用于标识你的用户，主要用于审计，或者用于区分Token颁发给谁
            // 但是注意RoleSessionName的长度和规则，不要有空格，只能有'-' '_' 字母和数字等字符
            // 具体规则请参考API文档中的格式要求
            string roleSessionName = "user" + userId;

            // 必须为 HTTPS
            try
            {
                AssumeRoleResponse stsResponse = assumeRole(AccessKeyID, AccessKeySecret, RoleArn, roleSessionName,
                        PolicyFile, ProtocolType.HTTPS, TokenExpireTime, REGION_CN_HANGZHOU);

                return new StsTokenModel()
                {
                    status = 200,
                    AccessKeyId = stsResponse.Credentials.AccessKeyId,
                    AccessKeySecret = stsResponse.Credentials.AccessKeySecret,
                    Expiration = stsResponse.Credentials.Expiration,
                    Security = stsResponse.Credentials.SecurityToken
                };

            }
            catch (ClientException e)
            {
                return new StsTokenModel() { status = Convert.ToInt32(e.ErrorCode) };
            }
        }


        public static StsTokenModel GetToken(int userId, string region)
        {
            // 只有 RAM用户（子账号）才能调用 AssumeRole 接口
            // 阿里云主账号的AccessKeys不能用于发起AssumeRole请求
            // 请首先在RAM控制台创建一个RAM用户，并为这个用户创建AccessKeys

            // RoleArn 需要在 RAM 控制台上获取
            // RoleSessionName 是临时Token的会话名称，自己指定用于标识你的用户，主要用于审计，或者用于区分Token颁发给谁
            // 但是注意RoleSessionName的长度和规则，不要有空格，只能有'-' '_' 字母和数字等字符
            // 具体规则请参考API文档中的格式要求
            string roleSessionName = "user" + userId;

            // 必须为 HTTPS
            try
            {
                AssumeRoleResponse stsResponse = assumeRole(AccessKeyID, AccessKeySecret, RoleArn, roleSessionName,
                        PolicyFile, ProtocolType.HTTPS, TokenExpireTime, region);

                return new StsTokenModel()
                {
                    status = 200,
                    AccessKeyId = stsResponse.Credentials.AccessKeyId,
                    AccessKeySecret = stsResponse.Credentials.AccessKeySecret,
                    Expiration = stsResponse.Credentials.Expiration,
                    Security = stsResponse.Credentials.SecurityToken
                };

            }
            catch (ClientException e)
            {
                return new StsTokenModel() { status = Convert.ToInt32(e.ErrorCode) };
            }
        }
    }
    
}