using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using YYLog.ClassLibrary;

public class WaveCellSMSSingleSender
{
    public static string SubAccountName = "";

    public static string Authorization = "";
    public WaveCellSMSResponseModels Send(string phone, string text)
    {
        WaveCellSMSModels sendModel = new WaveCellSMSModels();
        sendModel.destination = phone;
        sendModel.text = text;
        sendModel.encoding = "AUTO";
        string json = JsonConvert.SerializeObject(sendModel);
        string result = request(String.Format("https://api.wavecell.com/sms/v1/{0}/single", SubAccountName), json);
        WaveCellSMSResponseModels responseModels = new WaveCellSMSResponseModels();

        responseModels = JsonConvert.DeserializeObject<WaveCellSMSResponseModels>(result);
        return responseModels;
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
            //client.Headers.Remove("Accept");
            //client.Headers.Add("Accept", "*.*");
            client.Headers.Add("Authorization: "+ Authorization);
            //client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0; .NET4.0E; .NET4.0C; InfoPath.2; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; SE 2.X MetaSr 1.0)");
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