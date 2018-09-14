using System;

[Serializable]
public class InquiryTransferRequest
{
    public string merchantCode { get; set; }

    public string merchantOrderId { get; set; }

    public string signature { get; set; }
}