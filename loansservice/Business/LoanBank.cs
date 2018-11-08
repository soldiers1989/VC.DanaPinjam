using System;
using Newtonsoft.Json;
using RedisPools;
using YYLog.ClassLibrary;

public class LoanBank
{

    public string MD532(string str)
    {
        byte[] b = System.Text.Encoding.Default.GetBytes(str);

        b = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(b);
        string ret = "";
        for (int i = 0; i < b.Length; i++)
        {
            ret += b[i].ToString("x").PadLeft(2, '0');
        }
        return ret;
    }


    public InquriyTransferResponse DuitkuOrderStatusInquiryRequest(string orderId, string target)
    {
        HttpHelper http = new HttpHelper();

        InquiryTransferRequest request = new InquiryTransferRequest();
        request.merchantOrderId = orderId;
        switch (target.ToUpper())
        {
            case "B":
                request.merchantCode = ConfigHelper.GetBMerchantCode();
                request.signature = MD532(String.Format("{0}{1}{2}", request.merchantCode, orderId, ConfigHelper.GetDuitkuBApiSecretKey()));
                break;
            case "A":
            default:
                request.merchantCode = ConfigHelper.GetMerchantCode();
                request.signature = MD532(String.Format("{0}{1}{2}", request.merchantCode, orderId, ConfigHelper.GetDuitkuApiSecretKey()));
                break;
        }

        Log.WriteDebugLog("LoanBank::DuitkuOrderStatusInquiryRequest", "request info:{0}", JsonConvert.SerializeObject(request));

        //查询，验证转帐的银行信息
        return http.DuitkuInquiryTransactionStatusRequest(request);
    }

    public InquiryResponse DuitkuInquiryRequest(DebitUserRecord record)
    {
        HttpHelper http = new HttpHelper();

        InquiryRequest request = new InquiryRequest();
        request.amountTransfer = Convert.ToString(record.amountTransfer);
        request.bankAccount = record.bankAccount;
        request.bankCode = record.bankCode;
        request.purpose = record.purpose;
        request.senderId = record.userId;
        request.senderName = record.userName;
        request.InitSingature(record.target);

        Log.WriteDebugLog("LoanBank::Transfer", "request info:{0}", JsonConvert.SerializeObject(request));

        //查询，验证转帐的银行信息
        return http.DuitkuInquiryRequest(request);
    }
    public bool Transfer(DebitUserRecord record, out string errMsg)
    {
        Redis redis = new Redis();
        errMsg = String.Empty;
        string key = String.Format("lock_{0}", record.debitId);
        string retKey = String.Format("release_{0}", record.debitId);
        if (redis.LockTake(key, record.debitId, 300))
        {
            try
            {
                string transferResult = redis.StringGet(retKey);
                if (!String.IsNullOrEmpty(transferResult))
                {
                    InquiryResponse response = JsonConvert.DeserializeObject<InquiryResponse>(transferResult);
                    if (response.responseCode == "00")
                    {
                        Log.WriteDebugLog("LoanBank::Transfer", "[{0}]早已转帐成功：{1}", record.debitId, response.responseDesc);
                        return true;
                    }
                }
                else
                {
                    HttpHelper http = new HttpHelper();
                    Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 准备转帐，查询银行信息。", record.debitId);
                    //查询，验证转帐的银行信息
                    InquiryResponse response = null;
                    response = DuitkuInquiryRequest(record);

                    Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 核对银行帐号信息：{1}", record.debitId, JsonConvert.SerializeObject(response));

                    if (response.responseCode == "00")
                    {
                        Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 核对银行帐号信息，返回成功。", record.debitId);
                        Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 核对帐户名称，record：{1} ，response：{2}", record.debitId, record.userName.Trim().ToUpper(), response.accountName.Trim().ToUpper());
                        string bankUserName = response.accountName.Replace(" ", "").Trim().ToUpper();
                        string recordUserName = record.userName.Replace(" ", "").Trim().ToUpper();

                        ///相似度匹配
                        float rate = HttpHelper.Levenshtein(bankUserName, recordUserName);

                        if (bankUserName.IndexOf(recordUserName) > -1
                            || rate >= 0.7)
                        {
                            Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 帐户名称正确，初使化请求准备转帐。相似度：{1}%", record.debitId, rate * 100);
                            TransferRequest transferRequest = new TransferRequest();
                            transferRequest.accountName = record.userName.ToUpper();
                            transferRequest.amountTransfer = response.amountTransfer;
                            transferRequest.bankCode = response.bankCode.Trim();
                            transferRequest.custRefNumber = response.custRefNumber.Trim();
                            transferRequest.disburseId = response.disburseId;

                            transferRequest.purpose = record.purpose;
                            transferRequest.bankAccount = record.bankAccount;
                            transferRequest.InitSingature(record.target);

                            Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 开始转帐，渠道为：{1}，请求参数为:{2}", record.debitId, record.target, JsonConvert.SerializeObject(transferRequest));

                            response = http.DuitkuTransferRequest(transferRequest);

                            Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 转帐结果为：{1}", record.debitId, JsonConvert.SerializeObject(response));
                            if (response.responseCode == "00")
                            {
                                Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 转帐成功,将结果写入缓存，避免重复打款。：{1}", record.debitId, response.responseDesc);
                                redis.StringSet(retKey, JsonConvert.SerializeObject(response));
                                return true;
                            }
                            else
                            {
                                errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
                                Log.WriteErrorLog("LoanBank::Transfer", "[{0}] 转帐失败：{1}", record.debitId, response.responseDesc);
                                return false;
                            }
                        }
                        else
                        {
                            Log.WriteErrorLog("LoanBank::Transfer", "[{0}] 银行卡对应的名字与用户填写的名字不同：{1}!={2}，相似度：{3}", record.debitId, bankUserName, recordUserName, rate);
                            errMsg = String.Format("Bank Information Incorrect.accountName:{0} incorrect.", record.userName);

                            return false;
                        }
                    }
                    else
                    {
                        errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
                        Log.WriteErrorLog("LoanBank::Transfer", "[{0}] 转帐失败：{1}", record.debitId, response.responseDesc);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("LoanBank::Transfer", "[{0}] 转帐失败，发生异常，渠道：{1}, {2}", record.debitId, record.target, ex.Message);
            }
            finally
            {
                redis.LockRelease(key, record.debitId);
            }
            return false;
        }
        else
        {
            errMsg = "get lock fail.";
            return false;
        }
    }

    public bool CheckTransferStatus(string disburseId)
    {
        HttpHelper http = new HttpHelper();

        InquiryStatusRequest request = new InquiryStatusRequest();
        request.disburseId = disburseId;
        request.InitSingature();

        InquiryResponse response = http.DuitkuInquiryStatusRequest(request);

        Log.WriteDebugLog("LoanBank::CheckTransferStatus", "{0}", JsonConvert.SerializeObject(response));
        if (response.responseCode == "00")
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}