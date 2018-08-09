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
}

