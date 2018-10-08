using System;

[Serializable]
public class DebitUserRecord
{
    public int debitId;

    public string bankAccount;

    public string bankCode;

    public int userId;

    public string purpose = String.Empty;

    public string userName;

    public float amountTransfer;

    public string phone = String.Empty;

    ///标识
    public string target = String.Empty;
}


[Serializable]
public class DebitRecord
{
    public string phone { get; set; }
    public int debitId { get; set; }

    public int overdueDay { get; set; }

    public string money { get; set; }
    public int smsSendTimes { get; set; }
}
