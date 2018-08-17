using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    [Serializable]
    public class BankCode
    {
        public string bankCode = String.Empty;

        public string bankName = String.Empty;
    }

    [Serializable]
    public class NoticeModel
    {
        public string title = String.Empty;

        public string content = String.Empty;
    }
    [Serializable]
    public class SMSSendResultModel
    {
        public string request_id = String.Empty;

        public int status = -1;

        public string error_text = String.Empty;
    }

    [Serializable]
    public class SelectionModel
    {
        public int selectValue;

        public string selectText;

        public string selectType;
    }

    [Serializable]
    public class StsTokenModel
    {
        public int status { get; set; }

        public string AccessKeyId { get; set; }

        public string AccessKeySecret { get; set; }

        public string Security { get; set; }

        public string Expiration { get; set; }
    }
}