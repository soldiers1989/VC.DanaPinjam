using System;

public class InquiryStatusRequest
{
    public string disburseId = String.Empty;

    private int _userId = 0;
    public int userId {
        get {
            return _userId;
        }
    }

    private string _email = String.Empty;
    public string email 
    {
        get {
            return _email;
        }
    }

    private long _timestamp = 0L;
    public long timestamp 
    {
        get {
            return _timestamp;
        }
    }
    
    private string _signature = String.Empty;
    public string signature
    {
        get {
            return _signature;
        }
    }

    public void InitSingature()
    {
        _userId = ConfigHelper.GetDuitkuUserId();
        _email = ConfigHelper.GetDuitkuEmail();
        _timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        
        string signatureParam = String.Empty;
        signatureParam += email;
        signatureParam += timestamp;
        signatureParam += disburseId;
        signatureParam += ConfigHelper.GetDuitkuSecretKey();
        _signature = EncryptHelper.SHA256(signatureParam);
    }
}