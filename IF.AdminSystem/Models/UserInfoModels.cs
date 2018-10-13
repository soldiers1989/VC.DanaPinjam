using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    [Serializable]
    public class FaseBookUserInfo
    {
        public string id = String.Empty;
        public string birthday = String.Empty;
        public string first_name = String.Empty;
        public string gender = String.Empty;
        public string last_name = String.Empty;
        public string link = String.Empty;
        public string locale = String.Empty;
        public string name = String.Empty;
        public string timezone = String.Empty;
        public string verified = String.Empty;
    }

    ///联系人是否存在于通讯录中的请求包体
    [Serializable]
    public class CheckUserConactsRequestBodyModel
    {
        public int userId = -1;
        public int relationShip = 0;
        public string phone = String.Empty;
    }

    [Serializable]
    public class RequestBodyModel
    {
        public object data;
    }

    [Serializable]
    public class UserLocationModel
    {
        public int userId;
        public string locationX;
        public string locationY;
    }

    [Serializable]
    public class ConcatsdModel
    {
        /// <summary>
        /// 联系人名字
        /// </summary>
        public string name = String.Empty;
        /// <summary>
        /// 联系人电话
        /// </summary>
        public string phone = String.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CallRecord
    {
        /// <summary>
        /// 联系人名字
        /// </summary>
        public string name = String.Empty;
        /// <summary>
        /// 联系电话
        /// </summary>
        public string phone = String.Empty;
        /// <summary>
        /// 呼叫时间
        /// </summary>
        public string callTime = String.Empty;

        /// <summary>
        /// 通话时长
        /// </summary>
        public float duration = 0f;
    }

    [Serializable]
    public class UserCertificate
    {
        /// <summary>
        /// 
        /// </summary>
        public string resourceUrl = String.Empty;

        /// <summary>
        /// 
        /// </summary>
        public int userId = 0;

        /// <summary>
        /// 
        /// </summary>
        public int objectId = 0;

        /// <summary>
        /// 
        /// </summary>
        public int certId = -1;
    }

    /// <summary>
    /// 用户登录后信息
    /// </summary>
    [Serializable]
    public class UserInfoModel
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId = 0;

        /// <summary>
        /// 
        /// </summary>
        public string userName = String.Empty;

        /// <summary>
        /// 签名
        /// </summary>
        public string token = String.Empty;
    }

    /// <summary>
    /// 用户的所有信息
    /// </summary>
    [Serializable]
    public class UserAllInfoModel
    {
        [Display(Name = "资料完整性")]
        /// <summary>
        /// 资料完整性
        /// </summary>
        public int allPercent = 0;

        /// <summary>
        /// 联系人完整性
        /// </summary>
        public int contactPercent = 0;

        /// <summary>
        /// 工作资料完整性
        /// </summary>
        public int workingPercent = 0;

        /// <summary>
        /// 基本信息完整性
        /// </summary>
        public int personalPercent = 0;

        /// <summary>
        /// 证件完整性
        /// </summary>
        public int cardPercent = 0;

        /// <summary>
        /// 其它信息完整性
        /// </summary>
        public int otherInfoPercent = 0;

        /// <summary>
        /// 用户等级
        /// </summary>
        public int userLevel = 0;

        /// <summary>
        /// 用户关系人的联系信息
        /// </summary>
        public List<UserContactInfoModel> userContactInfo = new List<UserContactInfoModel>();

        /// <summary>
        /// 用户的工作信息
        /// </summary>
        public UserWorkingInfoModel userWorkingInfo = new UserWorkingInfoModel();

        /// <summary>
        /// 用户基本信息
        /// </summary>
        public UserPersonalInfoModel userPersonalInfo = new UserPersonalInfoModel();

        /// <summary>
        /// 用户证件照
        /// </summary>
        public UserCardPhotosModel userCards = new UserCardPhotosModel();

        /// <summary>
        /// 用户的其它信息
        /// </summary>
        public OtherInfo otherInfo = new OtherInfo();
    }

    /// <summary>
    /// 用户其它信息
    /// </summary>
    [Serializable]
    public class OtherInfo
    {
        /// <summary>
        /// 通话记录
        /// </summary>
        public int CallLogNum = 0;

        /// <summary>
        /// 通讯录
        /// </summary>
        public int ContactsNum = 0;

        /// <summary>
        /// 地址位置
        /// </summary>
        public int Location = 0;

        /// <summary>
        /// facebook同步状态 0 - 未同步 1 - 已同步
        /// </summary>
        public int FaceBookIsOk = 0;
    }

    /// <summary>
    /// 用户关系人的联系信息
    /// </summary>
    [Serializable]
    public class UserContactInfoModel
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public int id { get; set; } = 0;

        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; } = 0;

        /// <summary>
        /// 关系
        /// </summary>
        public int relationShip { get; set; } = 0;

        /// <summary>
        /// 关系人姓名
        /// </summary>
        public string relationUserName { get; set; } = String.Empty;

        /// <summary>
        /// 电话
        /// </summary>
        public string phone { get; set; } = String.Empty;

        /// <summary>
        /// 住址
        /// </summary>
        public string address { get; set; } = String.Empty;
    }

    /// <summary>
    /// 用户的工作信息
    /// </summary>
    [Serializable]
    public class UserWorkingInfoModel
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; } = 0;

        /// <summary>
        /// 工作类型
        /// </summary>
        public string typeOfWork { get; set; } = String.Empty;

        /// <summary>
        /// 月收入
        /// </summary>
        public string monthIncome { get; set; }

        /// <summary>
        /// 公司名
        /// </summary>
        public string companyName { get; set; } = String.Empty;

        /// <summary>
        /// 公司所在省份
        /// </summary>
        public string companyProvince { get; set; } = String.Empty;

        /// <summary>
        /// 公司所在城市
        /// </summary>
        public string companyCity { get; set; }

        /// <summary>
        /// 公司所在区
        /// </summary>
        public string companyDistrics { get; set; }

        /// <summary>
        /// 公司所在选区
        /// </summary>
        public string companyDistricts { get; set; }

        /// <summary>
        /// 公司地址
        /// </summary>
        public string address { get; set; } = String.Empty;

        /// <summary>
        /// 公司电话
        /// </summary>
        public string companyPhone { get; set; } = String.Empty;
    }

    /// <summary>
    /// 用户基本信息
    /// </summary>
    [Serializable]
    public class UserPersonalInfoModel
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; } = 0;

        /// <summary>
        /// 用户名
        /// </summary>
        public string userName { get; set; } = String.Empty;

        /// <summary>
        /// 完整的名称
        /// </summary>
        public string fullName { get; set; } = String.Empty;

        /// <summary>
        /// 妈妈的名字？
        /// </summary>
        public string motherName { get; set; } = String.Empty;

        /// <summary>
        /// 身份证卡号
        /// </summary>
        public string idCard { get; set; } = String.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public int gender { get; set; } = 0;

        /// <summary>
        /// 教育程度
        /// </summary>
        public string education { get; set; }

        /// <summary>
        /// 婚姻状态
        /// </summary>
        public string maritalStatus { get; set; }

        /// <summary>
        /// 有几个小孩
        /// </summary>
        public string numberOfChildren { get; set; }

        /// <summary>
        /// 居住城市
        /// </summary>
        public string residentialCity { get; set; } = String.Empty;

        /// <summary>
        /// 居住省份
        /// </summary>
        public string residentialProvince { get; set; } = String.Empty;

        /// <summary>
        /// 居住地区
        /// </summary>
        public int residentialDistrics { get; set; } = 0;

        /// <summary>
        /// 居住地选区
        /// </summary>
        public int residentialDistricts { get; set; } = 0;

        /// <summary>
        /// 地址
        /// </summary>
        public string address { get; set; } = String.Empty;

        /// <summary>
        /// 居住时长
        /// </summary>
        public string occupancyDuration { get; set; }

        /// <summary>
        /// 社交帐号
        /// </summary>
        public string socialAccounts { get; set; } = String.Empty;

        /// <summary>
        /// 生日
        /// </summary>
        public string birthday { get; set; } = String.Empty;
    }

    [Serializable]
    /// <summary>
    /// 用户银行卡信息
    /// </summary>
    public class UserBankInfoModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int bankId = 0;

        ///用户ID
        public int userId = 0;

        /// <summary>
        /// 银行名
        /// </summary>
        public string bankName = String.Empty;

        /// <summary>
        /// 开户支行
        /// </summary>
        public string subBankName = String.Empty;

        /// <summary>
        /// 银行帐号
        /// </summary>
        public string bankCode = String.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        public string contact = String.Empty;

        /// <summary>
        /// 联系人
        /// </summary>
        public string contactName = String.Empty;

        ///银行名对应的BankCode，在Main控制器中，GetBankCodes方法中
        public string bniBankCode = String.Empty;
    }

    [Serializable]
    public class UserCardPhotosModel
    {
        /// <summary>
        /// 身份证
        /// </summary>
        public string IdCardUrl = String.Empty;

        /// <summary>
        /// 工作证
        /// </summary>
        public string WorkinfoCardUrl = String.Empty;
    }

}