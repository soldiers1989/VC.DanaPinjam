using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    /// <summary>
    /// 主应用接口异常代码
    /// </summary>
    [Serializable]
    public class MainErrorModels
    {
        /// <summary>
        /// 逻辑错误
        /// </summary>
        public const int LOGIC_ERROR = -100001;

        /// <summary>
        /// 参数错误
        /// </summary>
        public const int PARAMETER_ERROR = -100002;

        /// <summary>
        /// 数据库请求异常
        /// </summary>
        public const int DATABASE_REQUEST_ERROR = -100003;

        /// <summary>
        /// 密码不一致
        /// </summary>
        public const int DIFFERENT_PASSWORD = -100004;
        
        /// <summary>
        /// 验证码错误
        /// </summary>
        public const int VERIFICATION_CODE_ERROR = -100005;

        /// <summary>
        /// 验证密码错误
        /// </summary>
        public const int USER_CERIFICATE_PASSWORD_FAIL = -100006;

        /// <summary>
        /// 手机已注册
        /// </summary>
        public const int THE_PHONE_ALREADY_REGISTERED = -100007;

        /// <summary>
        /// 手机号没有注册
        /// </summary>
        public const int THE_PHONE_NUMBER_NOT_REGISTERED = -100008;

        /// <summary>
        /// 没有这样的贷款组合
        /// </summary>
        public const int NO_SUCH_DEBIT_COMBINATION = -100009;

        /// <summary>
        /// 用户不存在
        /// </summary>
        public const int THE_USER_NOT_EXISTS = -100010;

        /// <summary>
        /// 银行信息不存在
        /// </summary>
        public const int THE_BANKINFO_NOT_EXISTS = -100011;

        /// <summary>
        /// 已提交或存在未还贷记录
        /// </summary>
        public const int ALREADY_SUMBIT_REQUEST_OR_NO_PAYBACK_DEBIT_RECORD = -100012;

        /// <summary>
        /// 还款凭证还没有提交
        /// </summary>
        public const int THE_PAYBACK_DEBIT_CERTIFICATE_NOT_SUBMIT = -100013;

        /// <summary>
        /// 状态为还款失败和申请成功的记录才可以申请还款
        /// </summary>
        public const int THE_DEBIT_RECORD_STATUS_FAIL = -100014;

        /// <summary>
        /// token验证失败
        /// </summary>
        public const int THE_TOKEN_VALIDATION_FAILED = -100015;

        /// <summary>
        /// 存在未还或申请中贷款记录，不允许修改资料
        /// </summary>
        public const int ALREADY_REQUEST_DEBIT_OR_HAVE_NO_PAYBACK = -100016;

        /// <summary>
        /// idcard号已被使用
        /// </summary>
        public const int IDCARD_ALREADY_USEED = -100017;

        public const int ALREADY_SUBMIT_REQUEST = -100018;
    }
}