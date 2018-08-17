using System;

public class DuitkuInquriyRequestModel
{
    public string bin { get; set; }

    public string vaNo { get; set; }

    public string session { get; set; }

    public string action { get; set; }

    public string signature { get; set; }

    public bool IsEmpty()
    {
        return String.IsNullOrEmpty(bin) ||String.IsNullOrEmpty(vaNo)||
        String.IsNullOrEmpty(session)||String.IsNullOrEmpty(action)||String.IsNullOrEmpty(signature);
    }
}

public class DuitkuInquriyResponseModel
{
    public string vaNo { get; set; }
    public string name { get; set; }
    public string amount { get; set; }
    
    ///设置为还款记录ID,对应IFUserPayBackDebitRecord 表中的ID。
    public string merchantOrderId { get; set; }
    public string statusCode { get; set; }
    public string statusMessage { get; set; }
}