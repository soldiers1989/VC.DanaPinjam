using System;

[Serializable]
public class InquriyTransferResponse
{
    public string merchantOrderId { get; set; }

    public string reference { get; set; }

    public string amount { get; set; }

    public string statusCode { get; set; }

    public string statusMessage { get; set; }
}