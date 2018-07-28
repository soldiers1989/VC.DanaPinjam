using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace DBMonoUtility
{
	class HttpReqHandler
	{
		private string SH = String.Empty;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="api"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public HttpResult Request(string api, string data)
		{
			try
			{
				HttpResult result = new HttpResult();

				long beginTime = DateTime.Now.Ticks;
				WebClient client = new WebClient();
				client.Encoding = Encoding.UTF8;
				client.Headers.Remove("Accept");
				client.Headers.Add("Accept", "*.*");
				client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET4.0E; .NET4.0C; InfoPath.2; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; SE 2.X MetaSr 1.0)");
				byte[] postData = Encoding.UTF8.GetBytes(data);
				client.Headers.Add("Content-Type: multipart/form-data");
				client.Headers.Add("ContentLength", Convert.ToString(postData.Length));
				string ret = client.UploadString(api, "POST", data);

				long.TryParse(client.ResponseHeaders["Pos"], out result.QueueId);
				long endTime = DateTime.Now.Ticks;

				TimeSpan ts = TimeSpan.FromTicks(endTime - beginTime);
				return result;
			}
			catch (Exception)
			{
			}
			return new HttpResult();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="api"></param>
		/// <returns></returns>
		public HttpResult RequestGet(string api)
		{
			HttpResult result = new HttpResult();
			try
			{
				long beginTime = DateTime.Now.Ticks;
				WebClient client = new WebClient();
				client.Encoding = Encoding.UTF8;
				client.Headers.Remove("Accept");
				client.Headers.Add("Accept", "application/json");
				client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
				result.Result = client.DownloadString(api);
				long.TryParse(Convert.ToString(client.ResponseHeaders["pos"]), out result.QueueId);
				long endTime = DateTime.Now.Ticks;
				TimeSpan ts = TimeSpan.FromTicks(endTime - beginTime);
				result.Time = ts.TotalMilliseconds;
			}
			catch (Exception ex)
			{
				result.ErrMsg = ex.Message;
			}
			return result;
		}

		public HttpResult RequestGetJson(string api)
		{
			HttpResult result = new HttpResult();
			try
			{
				long beginTime = DateTime.Now.Ticks;
				WebClient client = new WebClient();
				client.Encoding = Encoding.UTF8;
				//client.BaseAddress = new Helper().GetHttpBaseUrl();
				client.Headers.Remove("Accept");
				client.Headers.Add("Accept", "application/json");
				
				client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
				result.Result = client.DownloadString(api);
				long.TryParse(Convert.ToString(client.ResponseHeaders["pos"]), out result.QueueId);
				long endTime = DateTime.Now.Ticks;
				TimeSpan ts = TimeSpan.FromTicks(endTime - beginTime);
				result.Time = ts.TotalMilliseconds;
			}
			catch (Exception ex)
			{
				result.ErrMsg = ex.Message;
			}
			return result;
		}
	}
}
