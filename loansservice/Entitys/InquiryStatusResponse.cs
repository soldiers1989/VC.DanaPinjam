using System;

///状态查询返回结果
[Serializable]
public class InquiryStatusResponse
{
    public string email = String.Empty;

    public string bankCode = String.Empty;

    public string bankAccount = String.Empty;

    public string amountTransfer = String.Empty;

    public string accountName = String.Empty;

    public string custRefNumber = String.Empty;

    public string responseCode = String.Empty;

    public string responseDesc = String.Empty;
}