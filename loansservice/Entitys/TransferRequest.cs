using System;

public class TransferRequest
{
    public int disburseId = 0;

    private int _userId = 0;
    public int userId
    {
        get
        {
            return _userId;
        }
    }

    private string _email = String.Empty;
    public string email
    {
        get
        {
            return _email;
        }
    }

    private string _secretKey = String.Empty;
    public string secretKey
    {
        get { return _secretKey; }
    }

    public string bankCode = String.Empty;

    public string bankAccount = String.Empty;

    public string amountTransfer = String.Empty;

    public string accountName = String.Empty;

    public string custRefNumber = String.Empty;

    public string purpose = String.Empty;

    private long _timestamp = 0L;
    public long timestamp
    {
        get
        {
            return _timestamp;
        }
    }

    private string _signature = String.Empty;
    public string signature
    {
        get
        {
            return _signature;
        }
    }

    public void InitSingature(string target)
    {
        switch (Convert.ToString(target).ToUpper())
        {
            case "B":
                _userId = ConfigHelper.GetDuitkuBUserId();
                _email = ConfigHelper.GetDuitkuBEmail();
                _secretKey = ConfigHelper.GetDuitkuBSecretKey();
                break;
            case "A":
            default:
                _userId = ConfigHelper.GetDuitkuUserId();
                _email = ConfigHelper.GetDuitkuEmail();
                _secretKey = ConfigHelper.GetDuitkuSecretKey();
                break;
        }

        _timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;

        string signatureParam = String.Empty;
        signatureParam += email;
        signatureParam += timestamp;
        signatureParam += bankCode;
        signatureParam += bankAccount;
        signatureParam += accountName;
        signatureParam += custRefNumber;
        signatureParam += amountTransfer;
        signatureParam += purpose;
        signatureParam += disburseId;
        signatureParam += secretKey;
        //$paramSignature = $email . $timestamp . $bankCode . $bankAccount . $accountName . $custRefNumber . $amountTransfer . $purpose . $disburseId . $secretKey; 

        _signature = EncryptHelper.SHA256(signatureParam);
    }
}