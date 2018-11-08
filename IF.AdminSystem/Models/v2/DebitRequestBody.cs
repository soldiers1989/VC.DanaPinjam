using System;
using System.Collections.Generic;

namespace NF.AdminSystem.Models.v2
{
    public class InterestRateRequestBody
    {
        public float debitMoney { get; set; }

        public int debitPeriod { get; set; }
    }

    public class SubmitDebitRequestBody
    {
        public int userId { get; set; }

        public float debitMoney { get; set; }

        public string description { get; set; }

        public int bankId { get; set; }

        public int debitPeriod { get; set; }

        public string deviceId { get; set; }
    }

    public class DebitInfoRequestBody
    {
        public int debitId { get; set; }
    }
    public class DebitRecordsRequestBody
    {
        public int userId { get; set; }

        public int status { get; set; }

        public int index { get; set; }
    }

    public class DebitRecordLogModel
    {
        public int changeType { get; set; }
        public string changeTime { get; set; }
        public string description { get; set; }

        public string afterTime { get; set; }
    }
    public class DebitRecordLogResponse
    {
        public DebitInfoModel debitInfo { get; set; }
        public List<DebitRecordLogModel> logs { get; set; }
    }
}