public class AppSettingsModel
{
    public string RedisExchangeHosts { get; set; }
    public string RedisExchangePwd { get; set; }
    public string DBName { get; set; }
    public string SMSType {get;set;}
    public string SMSApiKey { get; set; }
    public string SMSApiSecret { get; set; }
    public string publicKey { get; set; }
    public string prefixNo { get; set; }
    public string duitkuKey { get; set; }
    public string PayMethod { get; set; }
    public string atmh5url {get;set;}

    public string WaveCellSMSAccountName {get;set;}

    public string WaveCellSMSAuthorization {get;set;}
}