using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    [Serializable]
    public class PayBackDebitModel
    {
        public int userId = 0;

        public int debitId = 0;

        public int status = 0;

        public float payBackDebitMoney = 0f;

        public string certificateUrl = String.Empty;

        public string statusTime = String.Empty;
    }

    [Serializable]
    public class DebitExtendModel
    {
        public int debitId;

        public int userId;

        public float debitMoney = 0f;

        public int debitPeroid = 0;

        public string target = String.Empty;

        public float extendFee = 0f;

        ///下次延期到的时间
        public string extendDay = String.Empty;

        public int status = -1;

        public float overdueMoney = 0;

        ///已还部份金额
        public float partMoney = 0f;
    }

    [Serializable]
    public class DebitInfoModel
    {
        public int debitId;

        public int userId;

        public string target = String.Empty;

        ///贷款金额
        public float debitMoney = 0f;

        ///已还部份金额
        public float partMoney = 0f;

        public int status = 0;

        public string createTime = String.Empty;

        public string releaseLoanTime = String.Empty;

        public string repaymentTime = String.Empty;

        public string auditTime = String.Empty;

        public string description = String.Empty;

        public int bankId = -1;

        public string certificate = String.Empty;

        public int debitPeroid = 0;

        public float payBackMoney = 0f;

        public float overdueMoney = 0f;

        public int overdueDay = 0;

        public float overdueInterset = 0.02f;

        public float dayInterset = 0f;

        public string auditInfo = String.Empty;
    }

    [Serializable]
    public class DebitInfo
    {
        /// <summary>
        /// 贷款金额
        /// </summary>
        public float debitMoney = 0;

        /// <summary>
        /// 贷款周期
        /// </summary>
        public int debitPeriod = 7;

        /// <summary>
        /// 还款总额
        /// </summary>
        public float payBackMoney = 0f;

        /// <summary>
        /// 日息
        /// </summary>
        public float dailyInterest = 0f;

        /// <summary>
        /// 逾期后日息
        /// </summary>
        public float overdueDayInterest = 0f;

        /// <summary>
        /// 描述
        /// </summary>
        public string description = String.Empty;

        /// <summary>
        /// 费用描述
        /// </summary>
        public string adminFee = String.Empty;

        /// <summary>
        /// 手续费
        /// </summary>
        public float debitFee = 0f;

        /// <summary>
        /// 实际到帐
        /// </summary>
        public float actualMoney = 0f;

        ///显示状态 0 - 不可选 1 - 可选
        public int displayStyle = 1;
    }
}