using System;
using Newtonsoft.Json;
using YYLog.ClassLibrary;

public class LoanBank
{
    public bool Transfer(DebitUserRecord record, out string errMsg)
    {
        errMsg = String.Empty;

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
        InquiryResponse response = http.DuitkuInquiryRequest(request);

        Log.WriteDebugLog("LoanBank::Transfer step 1 check bank account :", "{0}", JsonConvert.SerializeObject(response));

        if (response.responseCode == "00")
        {
            if (response.accountName.Trim().ToUpper() == record.userName.Trim().ToUpper())
            {
                TransferRequest transferRequest = new TransferRequest();
                transferRequest.accountName = response.accountName.Trim();
                transferRequest.amountTransfer = response.amountTransfer;
                transferRequest.bankCode = response.bankCode.Trim();
                transferRequest.custRefNumber = response.custRefNumber.Trim();
                transferRequest.disburseId = response.disburseId;

                transferRequest.purpose = record.purpose;
                transferRequest.bankAccount = record.bankAccount;
                transferRequest.InitSingature();

                Log.WriteDebugLog("LoanBank::Transfer", "step 2 Transfer money to bank account,request param:{0}", JsonConvert.SerializeObject(transferRequest));

                response = http.DuitkuTransferRequest(transferRequest);
                if (response.responseCode == "00")
                {
                    Log.WriteDebugLog("LoanBank::Transfer", response.responseDesc);
                    return true;
                }
                else
                {
                    errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
                    Log.WriteErrorLog("LoanBank::Transfer", response.responseDesc);
                    return false;
                }
            }
            else
            {
                Log.WriteErrorLog("LoanBank::Transfer", "银行卡对应的名字与用户填写的名字不同：{0}!={1}", response.accountName, record.userName);
                errMsg = String.Format("the bank accountName:{0} incorrect.", record.userName);

                return false;
            }
        }
        else
        {
            errMsg = String.Format("{0}({1})", response.responseDesc, response.responseCode);
            Log.WriteErrorLog("LoanBank::Transfer", response.responseDesc);
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