using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace NF.AdminSystem.Providers.v2
{
	public static class HttpWebRequestHelper
    {
        /// <summary>
        /// post提交
        /// </summary>
        /// <param name="RESTServer_URI"></param>
        /// <param name="PostParametersString"></param>
        /// <param name="errorMSG"></param>
        /// <returns></returns>
        public static string Post(string RESTServer_URI, string PostParametersString, out string errorMSG)
        {
            string backstr = "";
            errorMSG = "";
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(PostParametersString);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(RESTServer_URI);
                myRequest.Method = "POST";
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.ContentLength = data.Length;
                Stream newStream = myRequest.GetRequestStream();
                // 发送POST请求数据
                newStream.Write(data, 0, data.Length);
                newStream.Close();
                //接受返回的数据
                HttpWebResponse res = (HttpWebResponse)myRequest.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                backstr = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                errorMSG = ex.Message;
            }

            return backstr;
        }
        public static string Post(string RESTServer_URI, string PostParametersString)
        {
            string backstr = "";
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(PostParametersString);
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(RESTServer_URI);
                myRequest.Method = "POST";
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.ContentLength = data.Length;
                Stream newStream = myRequest.GetRequestStream();
                // 发送POST请求数据
                newStream.Write(data, 0, data.Length);
                newStream.Close();
                //接受返回的数据
                HttpWebResponse res = (HttpWebResponse)myRequest.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                backstr = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
            }
            return backstr;
        }
        
        /// <summary>
        /// Get提交
        /// </summary>
        /// <param name="getstr"></param>
        /// <returns></returns>
        public static string Get(string getstr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getstr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        /// <summary>
        /// Get提交
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="postDataStr"></param>
        /// <returns></returns>
        public static string Get(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
    }
}
