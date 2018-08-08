using System;

///银行帐号查询返回结果
[Serializable]
public class InquiryResponse
{
    public string email = String.Empty;

    public string bankCode = String.Empty;

    public float amountTransfer = 0f;

    public string accountName = String.Empty;

    public string custRefNumber = String.Empty;

    public int disburseId = -1;

    public bool isNameSimilar = false;

    public string responseCode = String.Empty;

    public string responseDesc = String.Empty;
}