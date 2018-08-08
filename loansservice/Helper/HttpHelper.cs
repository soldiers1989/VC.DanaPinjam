using System;
using System.Net;
using System.Text;

using YYLog.ClassLibrary;

public class HttpHelper
{
    private string SH = String.Empty;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="api"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public InquiryResponse DuitkuInquiryRequest(DebitUserRecord record)
		{
			InquiryResponse result = new InquiryResponse();
			try
			{
                request("/webapi/api/disbursement/inquiry", "");
			}
			catch (Exception)
			{
			}
			return result;
		}

        string request(string api, string data)
		{
            string result = String.Empty;
			try
			{
				long beginTime = DateTime.Now.Ticks;
				WebClient client = new WebClient();
				client.Encoding = Encoding.UTF8;
				client.Headers.Remove("Accept");
				client.Headers.Add("Accept", "*.*");
				client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET4.0E; .NET4.0C; InfoPath.2; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; SE 2.X MetaSr 1.0)");
				byte[] postData = Encoding.UTF8.GetBytes(data);
				client.Headers.Add("Content-Type: application/json");
				client.Headers.Add("ContentLength", Convert.ToString(postData.Length));
				result = client.UploadString(api, "POST", data);

				return result;
			}
			catch (Exception ex)
			{
				
			}
            
            return String.Empty;
		}
}