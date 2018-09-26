using System;
public class WaveCellSMSModels
{
    public string source = "DANA PINJAM";

    public string destination;

    //public string clientMessageId;

    public string text;

    public string encoding = "AUTO";
}

public class WaveCellSMSResponseModels
{
    public string umid = String.Empty;
    public string clientMessageId = String.Empty;

    public string destination = String.Empty;

    public string encoding = String.Empty;

    public WaveCellSMSSendStatus status;
}

public class WaveCellSMSSendStatus
{
    public string code = String.Empty;
    public string description = String.Empty;
}