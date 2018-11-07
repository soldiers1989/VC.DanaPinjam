using System;

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
}