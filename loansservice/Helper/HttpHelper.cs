using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

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