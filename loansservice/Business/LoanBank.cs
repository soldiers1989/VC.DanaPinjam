using System;
using Newtonsoft.Json;
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


    public InquriyTransferResponse DuitkuOrderStatusInquiryRequest(string orderId, string merchantCode)
    {
        HttpHelper http = new HttpHelper();

        InquiryTransferRequest request = new InquiryTransferRequest();
        request.merchantOrderId = orderId;
        request.merchantCode = merchantCode;
        request.signature = MD532(String.Format("{0}{1}{2}", request.merchantCode, orderId, ConfigHelper.GetDuitkuApiSecretKey()));
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
        request.InitSingature();

        Log.WriteDebugLog("LoanBank::Transfer", "request info:{0}", JsonConvert.SerializeObject(request));

        //查询，验证转帐的银行信息
        return http.DuitkuInquiryRequest(request);
    }
    public bool Transfer(DebitUserRecord record, out string errMsg)
    {
        errMsg = String.Empty;

        HttpHelper http = new HttpHelper();
        Log.WriteDebugLog("LoanBank::Transfer", "准备转帐，查询银行信息。{0}", record.debitId);
        //查询，验证转帐的银行信息
        InquiryResponse response = DuitkuInquiryRequest(record);

        Log.WriteDebugLog("LoanBank::Transfer", "核对银行帐号信息：{0}", JsonConvert.SerializeObject(response));

        if (response.responseCode == "00")
        {
            Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 核对银行帐号信息，返回成功。", record.debitId);
            Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 核对帐户名称，record：{0} ，response：{1}", record.userName.Trim().ToUpper(), response.accountName.Trim().ToUpper());
            string bankUserName = response.accountName.Replace(" ", "").Trim().ToUpper();
            string recordUserName = record.userName.Replace(" ", "").Trim().ToUpper();

            float rate = HttpHelper.Levenshtein(bankUserName, recordUserName);

            if (bankUserName.IndexOf(recordUserName) > -1
                || rate >= 0.7)
            {
                Log.WriteDebugLog("LoanBank::Transfer", "[{0}] 帐户名称正确，初使化请求准备转帐。相似度：{1}", record.debitId,rate);
                TransferRequest transferRequest = new TransferRequest();
                transferRequest.accountName = record.userName.ToUpper();
                transferRequest.amountTransfer = response.amountTransfer;
                transferRequest.bankCode = response.bankCode.Trim();
                transferRequest.custRefNumber = response.custRefNumber.Trim();
                transferRequest.disburseId = response.disburseId;

                transferRequest.purpose = record.purpose;
                transferRequest.bankAccount = record.bankAccount;
                transferRequest.InitSingature();

                Log.WriteDebugLog("LoanBank::Transfer", "开始转帐，请求参数为:{0}", JsonConvert.SerializeObject(transferRequest));

                response = http.DuitkuTransferRequest(transferRequest);

                Log.WriteDebugLog("LoanBank::Transfer", "转帐结果为：{0}", JsonConvert.SerializeObject(response));
                if (response.responseCode == "00")
                {
                    Log.WriteDebugLog("LoanBank::Transfer", "转帐成功：{0}", response.responseDesc);
                    return true;
                }
                else
                {
                    errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
                    Log.WriteErrorLog("LoanBank::Transfer", "转帐失败：{0}", response.responseDesc);
                    return false;
                }
            }
            else
            {
                Log.WriteErrorLog("LoanBank::Transfer", "银行卡对应的名字与用户填写的名字不同：{0}!={1}，相似度：{2}", bankUserName, recordUserName,rate);
                errMsg = String.Format("Bank Information Incorrect.accountName:{0} incorrect.", record.userName);

                return false;
            }
        }
        else
        {
            errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
            Log.WriteErrorLog("LoanBank::Transfer", "转帐失败：{0}", response.responseDesc);
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