using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

using YYLog.ClassLibrary;

public class HttpHelper
{
    private string SH = String.Empty;

    public static float Levenshtein(string str1, string str2)
    {
        char[] char1 = str1.ToCharArray();
        char[] char2 = str2.ToCharArray();
        //计算两个字符串的长度。  
        int len1 = char1.Length;
        int len2 = char2.Length;
        //建二维数组，比字符长度大一个空间  
        int[,] dif = new int[len1 + 1, len2 + 1];
        //赋初值  
        for (int a = 0; a <= len1; a++)
        {
            dif[a, 0] = a;
        }
        for (int a = 0; a <= len2; a++)
        {
            dif[0, a] = a;
        }
        //计算两个字符是否一样，计算左上的值  
        int temp;
        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                if (char1[i - 1] == char2[j - 1])
                {
                    temp = 0;
                }
                else
                {
                    temp = 1;
                }
                //取三个值中最小的  
                dif[i, j] = Min(dif[i - 1, j - 1] + temp, dif[i, j - 1] + 1, dif[i - 1, j] + 1);
            }
        }
        //计算相似度  
        float similarity = 1 - (float)dif[len1, len2] / Math.Max(len1, len2);
        return similarity;
    }

    /// <summary>
    /// 求最小值
    /// </summary>
    /// <param name="nums"></param>
    /// <returns></returns>
    private static int Min(params int[] nums)
    {
        int min = int.MaxValue;
        foreach (int item in nums)
        {
            if (min > item)
            {
                min = item;
            }
        }
        return min;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="api"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public InquiryResponse DuitkuInquiryRequest(InquiryRequest inquiryRequest)
    {
        InquiryResponse result = new InquiryResponse();
        try
        {
            string data = JsonConvert.SerializeObject(inquiryRequest);

            string response = request("https://passport.duitku.com/webapi/api/disbursement/inquiry", data);

            if (String.IsNullOrEmpty(response))
            {
                return new InquiryResponse();
            }
            else
            {
                result = JsonConvert.DeserializeObject<InquiryResponse>(response);
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("HttpHelper::DuitkuInquiryRequest", ex.Message);
        }
        return result;
    }

    public InquriyTransferResponse DuitkuInquiryTransactionStatusRequest(InquiryTransferRequest inquiryRequest)
    {
        InquriyTransferResponse result = new InquriyTransferResponse();
        try
        {
            string data = JsonConvert.SerializeObject(inquiryRequest);

            string response = request("https://passport.duitku.com/webapi/api/merchant/transactionStatus", data);

            if (String.IsNullOrEmpty(response))
            {
                return result;
            }
            else
            {
                result = JsonConvert.DeserializeObject<InquriyTransferResponse>(response);
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("HttpHelper::DuitkuInquiryTransactionStatusRequest", ex.Message);
        }
        return result;
    }
    public InquiryResponse DuitkuInquiryStatusRequest(InquiryStatusRequest inquiryRequest)
    {
        InquiryResponse result = new InquiryResponse();
        try
        {
            string data = JsonConvert.SerializeObject(inquiryRequest);

            string response = request("https://passport.duitku.com/webapi/api/disbursement/inquirystatus", data);

            if (String.IsNullOrEmpty(response))
            {
                return new InquiryResponse();
            }
            else
            {
                result = JsonConvert.DeserializeObject<InquiryResponse>(response);
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("HttpHelper::DuitkuInquiryStatusRequest", ex.Message);
        }
        return result;
    }

    public InquiryResponse DuitkuTransferRequest(TransferRequest transferRequest)
    {
        InquiryResponse result = new InquiryResponse();
        try
        {
            string data = JsonConvert.SerializeObject(transferRequest);

            string response = request("https://passport.duitku.com/webapi/api/disbursement/transfer", data);

            if (String.IsNullOrEmpty(response))
            {
                return new InquiryResponse();
            }
            else
            {
                result = JsonConvert.DeserializeObject<InquiryResponse>(response);
            }
        }
        catch (Exception ex)
        {
            Log.WriteErrorLog("HttpHelper::DuitkuTransferRequest", ex.Message);
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
            client.Proxy = null;
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
            Log.WriteErrorLog("HttpHelper::request", ex.Message);
        }

        return String.Empty;
    }
}