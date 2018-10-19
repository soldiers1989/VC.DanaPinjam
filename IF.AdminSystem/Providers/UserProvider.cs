using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DBMonoUtility;
using YYLog.ClassLibrary;
using System.Collections;
using System.Data;
using NF.AdminSystem.Models;
using NF.AdminSystem.Controllers;
using Newtonsoft.Json;
using RedisPools;

namespace NF.AdminSystem.Providers
{
    public class UserProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="phone"></param>
        /// <param name="pass"></param>
        /// <param name="regType"></param>
        /// <param name="qudao"></param>
        /// <returns></returns>
        public static DataProviderResultModel UserRegister(string userName, string phone, string pass, int regType, string qudao)
        {
            //Provider
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserInfoModel userInfo = new UserInfoModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string flag = String.Empty;
                flag = HelperProvider.GetRamdomFlag(6);
                pass = HelperProvider.MD5Encrypt32(pass + flag);
                pc.Add("@sUserName", userName);
                pc.Add("@sPhone", phone);
                pc.Add("@sPass", pass);
                pc.Add("@sFlag", flag);
                pc.Add("@iRegType", Convert.ToString(regType));
                pc.Add("@sQudao", qudao);
                Hashtable outAl = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_user_register", pc.GetParams(), out outAl);

                if (null != dt && dt.Rows.Count > 0)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    if (result.result == 1)
                    {
                        int.TryParse(Convert.ToString(dt.Rows[0][1]), out userInfo.userId);
                        userInfo.userName = userName;
                        userInfo.token = Guid.NewGuid().ToString();
                        result.data = userInfo;

                    }
                    result.message = Convert.ToString(dt.Rows[0][2]);
                }
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("UserProvider::UserRegister", "注册失败：{0}|{1}|{2}|{3}|{4}，异常：{5}", userName, phone, pass, regType, qudao, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="pass"></param>
        /// <param name="loginType"></param>
        /// <returns></returns>
        public static DataProviderResultModel UserLogin(string phone, string pass, string qudao, int loginType)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserInfoModel userInfo = new UserInfoModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string flag = String.Empty;

                string sqlStr = "select min(flag) from IFUsers where phone = @iPhone";
                pc.Add("@iPhone", phone);

                flag = Convert.ToString(dbo.GetScalar(sqlStr, pc.GetParams(true)));

                if (String.IsNullOrEmpty(flag))
                {
                    result.result = MainErrorModels.THE_PHONE_NUMBER_NOT_REGISTERED;
                    result.message = "The phone number not exists.";
                    return result;
                }
                pass = HelperProvider.MD5Encrypt32(pass + flag);
                pc.Add("@sUserName", phone);
                pc.Add("@sPass", pass);
                pc.Add("@sQudao", qudao);
                pc.Add("@iloginType", Convert.ToString(loginType));
                Hashtable outAl = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_user_login_v2", pc.GetParams(), out outAl);

                int ret = 0;
                string message = String.Empty;
                if (null != dt && dt.Rows.Count > 0)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    result.message = Convert.ToString(dt.Rows[0][1]);
                    if (result.result > 0)
                    {
                        int.TryParse(Convert.ToString(dt.Rows[0][2]), out userInfo.userId);
                        userInfo.userName = Convert.ToString(dt.Rows[0][3]);
                        result.data = userInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("UserProvider::UserLogin", "注册失败：{0}|{1}|{2}，异常：{3}", phone, pass, loginType, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DataProviderResultModel GetUserBankInfo(string userId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserBankInfoModel bankInfo = new UserBankInfoModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = "select bankId,BankName,SubBankName,BankCode,Contact,ContactName,BNICode from IFUserBankInfo where userId = @iUserId order by updateTime desc limit 1";
                pc.Add("@iUserId", userId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0]["bankId"]), out bankInfo.bankId);
                    bankInfo.bankCode = Convert.ToString(dt.Rows[0]["bankCode"]);
                    bankInfo.bankName = Convert.ToString(dt.Rows[0]["BankName"]);
                    bankInfo.subBankName = Convert.ToString(dt.Rows[0]["SubBankName"]);
                    bankInfo.contact = Convert.ToString(dt.Rows[0]["Contact"]);
                    bankInfo.contactName = Convert.ToString(dt.Rows[0]["ContactName"]);
                    bankInfo.bniBankCode = Convert.ToString(dt.Rows[0]["BNICode"]);
                    result.result = Result.SUCCESS;
                    result.data = bankInfo;
                }
                else
                {
                    result.result = Result.SUCCESS;
                    bankInfo.bankId = -1;
                    result.data = bankInfo;
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "Database logic error from the GetUserBankInfo function.";
                Log.WriteErrorLog("UserProvider::GetUserBankInfo", "获取失败：{0}，异常：{1}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DataProviderResultModel GetUserAllInfo(string userId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserAllInfoModel userInfo = new UserAllInfoModel();
            int tmp = 0;
            float ftmp = 0f;
            try
            {
                UserWorkingInfoModel workingInfo = new UserWorkingInfoModel();
                UserPersonalInfoModel personalInfo = new UserPersonalInfoModel();

                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select userId,userName,fullName,ifnull(birthday,'') birthday,motherName,socialAccounts,phone,idcard,sex,occupancyDuration,education,numberOfChildren,
                    ifnull(residentialAddress,'') residentialAddress,ifnull(residentialDistricts, -1) residentialDistricts,ifnull(residentialDistrics,-1) residentialDistrics, ifnull(residentialCity,0) residentialCity,
                    ifnull(residentialProvince,'') residentialProvince, ifnull(maritalStatus,-1) maritalStatus, RegTime, ifnull(CanDebitMoney, 0) CanDebitMoney, 
                    ifnull(AlreadyDebitMoney,0) AlreadyDebitMoney,ifnull(FrozenDebitMoney,0) FrozenDebitMoney,ifnull(DebitCount,0) DebitCount,ifnull(typeOfWork, '') typeOfWork, ifnull(monthIncome,0) monthIncome,companyName,
                    ifnull(companyProvince, '') companyProvince, ifnull(companyCity,-1) companyCity, ifnull(companyDistrics,-1) companyDistrics,
                    ifnull(companyDistricts, -1) companyDistricts,companyAddress,companyPhone,ifnull(level,0) userLevel,
                    ifnull(contactUploadNumber,0) contactUploadNumber,ifnull(callRecordUploadNumber,0) callRecordUploadNumber,ifnull(locationX,-1) locationX,ifnull(locationY,-1) locationY,sum(if(facebookId is null, 0, 1)) facebookId
                    from IFUsers where userId  = @iUserId;";

                pc.Add("@iUserId", userId);

                int workInfoNumber = 0;
                int personaNumber = 0;
                int contactNumber = 0;
                int otherInfoNumber = 0;

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams(true));
                if (dt.Rows.Count == 1)
                {
                    #region workingInfo init...

                    int.TryParse(Convert.ToString(dt.Rows[0]["userLevel"]), out tmp);
                    userInfo.userLevel = tmp;

                    workingInfo.address = Convert.ToString(dt.Rows[0]["companyAddress"]);
                    //userInfo.allPercent++;
                    workInfoNumber++;
                    if (!String.IsNullOrEmpty(workingInfo.address))
                    {
                        userInfo.workingPercent++;
                    }

                    workingInfo.companyPhone = Convert.ToString(dt.Rows[0]["companyPhone"]);
                    workInfoNumber++;
                    if (!String.IsNullOrEmpty(workingInfo.companyPhone))
                    {
                        userInfo.workingPercent++;
                    }

                    workInfoNumber++;
                    workingInfo.companyName = Convert.ToString(dt.Rows[0]["companyName"]);
                    if (!String.IsNullOrEmpty(workingInfo.companyName))
                    {
                        userInfo.workingPercent++;
                    }

                    workInfoNumber++;
                    workingInfo.companyProvince = Convert.ToString(dt.Rows[0]["companyProvince"]);
                    if (!String.IsNullOrEmpty(workingInfo.companyProvince))
                    {
                        userInfo.workingPercent++;
                    }

                    //int.TryParse(Convert.ToString(dt.Rows[0]["monthIncome"]), out tmp);
                    workingInfo.monthIncome = Convert.ToString(dt.Rows[0]["monthIncome"]);
                    if (String.IsNullOrEmpty(workingInfo.monthIncome))
                    {
                        workingInfo.monthIncome = "0";
                    }

                    workInfoNumber++;
                    workingInfo.typeOfWork = Convert.ToString(dt.Rows[0]["typeOfWork"]);
                    if (!String.IsNullOrEmpty(workingInfo.typeOfWork))
                    {
                        userInfo.workingPercent++;
                    }
                    int.TryParse(userId, out tmp);
                    workingInfo.userId = tmp;

                    #endregion

                    personaNumber++;
                    personalInfo.fullName = Convert.ToString(dt.Rows[0]["fullName"]);
                    if (!String.IsNullOrEmpty(personalInfo.fullName))
                    {
                        userInfo.personalPercent++;
                    }

                    personalInfo.motherName = Convert.ToString(dt.Rows[0]["motherName"]);
                    personalInfo.socialAccounts = Convert.ToString(dt.Rows[0]["socialAccounts"]);

                    personaNumber++;
                    personalInfo.address = Convert.ToString(dt.Rows[0]["residentialAddress"]);
                    if (!String.IsNullOrEmpty(personalInfo.address))
                    {
                        userInfo.personalPercent++;
                    }

                    personaNumber++;
                    personalInfo.idCard = Convert.ToString(dt.Rows[0]["idCard"]);
                    if (!String.IsNullOrEmpty(personalInfo.idCard))
                    {
                        userInfo.personalPercent++;
                    }

                    personaNumber++;
                    personalInfo.residentialProvince = Convert.ToString(dt.Rows[0]["residentialProvince"]);
                    if (!String.IsNullOrEmpty(personalInfo.residentialProvince))
                    {
                        userInfo.personalPercent++;
                    }

                    personalInfo.residentialCity = Convert.ToString(dt.Rows[0]["residentialCity"]);


                    //int.TryParse(Convert.ToString(dt.Rows[0]["occupancyDuration"]), out tmp);
                    personalInfo.occupancyDuration = Convert.ToString(dt.Rows[0]["occupancyDuration"]);
                    if (String.IsNullOrEmpty(personalInfo.occupancyDuration))
                    {
                        personalInfo.occupancyDuration = "0";
                    }
                    //int.TryParse(Convert.ToString(dt.Rows[0]["numberOfChildren"]), out tmp);
                    personalInfo.numberOfChildren = Convert.ToString(dt.Rows[0]["numberOfChildren"]);
                    if (String.IsNullOrEmpty(personalInfo.numberOfChildren))
                    {
                        personalInfo.numberOfChildren = "0";
                    }
                    //int.TryParse(Convert.ToString(dt.Rows[0]["maritalStatus"]), out tmp);
                    personalInfo.maritalStatus = Convert.ToString(dt.Rows[0]["maritalStatus"]);
                    if (String.IsNullOrEmpty(personalInfo.maritalStatus))
                    {
                        personalInfo.maritalStatus = "0";
                    }
                    //int.TryParse(Convert.ToString(dt.Rows[0]["education"]), out tmp);
                    personalInfo.education = Convert.ToString(dt.Rows[0]["education"]);
                    if (String.IsNullOrEmpty(personalInfo.education))
                    {
                        personalInfo.education = "0";
                    }
                    personalInfo.birthday = Convert.ToString(dt.Rows[0]["birthday"]);
                    personaNumber++;
                    if (!String.IsNullOrEmpty(personalInfo.birthday))
                    {
                        userInfo.personalPercent++;
                    }

                    int.TryParse(Convert.ToString(dt.Rows[0]["sex"]), out tmp);
                    personaNumber++;
                    personalInfo.gender = tmp;
                    if (personalInfo.gender > 0)
                    {
                        userInfo.personalPercent++;
                    }

                    int.TryParse(userId, out tmp);
                    personalInfo.userId = tmp;
                    personalInfo.userName = Convert.ToString(dt.Rows[0]["userName"]);

                    otherInfoNumber++;
                    int.TryParse(Convert.ToString(dt.Rows[0]["contactUploadNumber"]), out tmp);
                    OtherInfo otherInfo = new OtherInfo();
                    otherInfo.ContactsNum = tmp;
                    if (otherInfo.ContactsNum > 0)
                    {
                        userInfo.otherInfoPercent++;
                    }

                    otherInfoNumber++;
                    int.TryParse(Convert.ToString(dt.Rows[0]["callRecordUploadNumber"]), out tmp);
                    otherInfo.CallLogNum = tmp;
                    if (otherInfo.CallLogNum > 0)
                    {
                        userInfo.otherInfoPercent++;
                    }


                    otherInfoNumber++;
                    float x, y;

                    float.TryParse(Convert.ToString(dt.Rows[0]["locationX"]), out ftmp);
                    x = ftmp;
                    float.TryParse(Convert.ToString(dt.Rows[0]["locationY"]), out ftmp);
                    y = ftmp;

                    if (x != -1 && y != -1)
                    {
                        otherInfo.Location = 1;
                        userInfo.otherInfoPercent++;
                    }


                    int.TryParse(Convert.ToString(dt.Rows[0]["facebookId"]), out tmp);
                    otherInfo.FaceBookIsOk = tmp;
                    userInfo.otherInfo = otherInfo;
                }

                sqlStr = @"select id,userId,relationShip,relationUserName,phone,address,
                            (select count(1) from IFUserContacts a where a.phone = b.phone and a.recordType = 1) isComplete
                            from IFUserContactInfo b where userId = @iUserId order by relationShip limit 4";
                pc.Add("@iUserId", userId);

                DataTable relationShip = dbo.GetTable(sqlStr, pc.GetParams(true));

                if (null != relationShip && relationShip.Rows.Count > 0)
                {
                    for (int i = 0; i < relationShip.Rows.Count; i++)
                    {
                        UserContactInfoModel contactInfo = new UserContactInfoModel();
                        int.TryParse(Convert.ToString(relationShip.Rows[i]["id"]), out tmp);
                        contactInfo.id = tmp;
                        int.TryParse(Convert.ToString(relationShip.Rows[i]["userId"]), out tmp);
                        contactInfo.userId = tmp;

                        contactInfo.phone = Convert.ToString(relationShip.Rows[i]["phone"]);
                        if (!String.IsNullOrEmpty(contactInfo.phone))
                        {
                            userInfo.contactPercent++;
                            contactInfo.isComplete = 1;
                        }
                        else
                        {
                            contactInfo.isComplete = 0;
                        }
                        contactNumber++;

                        /*
                        contactInfo.relationUserName = Convert.ToString(relationShip.Rows[i]["relationUserName"]);
                        if (!String.IsNullOrEmpty(contactInfo.relationUserName))
                        {
                            userInfo.contactPercent++;
                        }
                        contactNumber++;
                         */

                        int.TryParse(Convert.ToString(relationShip.Rows[i]["relationShip"]), out tmp);
                        contactInfo.relationShip = tmp;

                        //int.TryParse(Convert.ToString(relationShip.Rows[i]["isComplete"]), out tmp);
                        //contactInfo.isComplete = tmp;
                        //if (contactInfo.relationShip > 0)
                        //{
                        //    userInfo.contactPercent++;
                        //}
                        //contactNumber++;

                        userInfo.userContactInfo.Add(contactInfo);
                    }
                }
                int already = userInfo.userContactInfo.Count;
                for (int i = 0; i < 4 - already; i++)
                {
                    userInfo.userContactInfo.Add(new UserContactInfoModel { id = -1,userId = Convert.ToInt32(userId),relationShip = (i+1) });
                    contactNumber += 2;
                }

                int cardNumber = 2;
                sqlStr = "select url from IFCertificate where certificateType = @iCertificateType and certificateUserId = @iCertificateUserId limit 1";
                pc.Add("@iCertificateType", 1);
                pc.Add("@iCertificateUserId", userId);


                string url = Convert.ToString(dbo.GetScalar(sqlStr, pc.GetParams(true)));
                if (!String.IsNullOrEmpty(url))
                {
                    userInfo.userCards.IdCardUrl = url;
                    userInfo.cardPercent++;
                }
                sqlStr = "select url from IFCertificate where certificateType = @iCertificateType and certificateUserId = @iCertificateUserId limit 1";
                pc.Add("@iCertificateType", 2);
                pc.Add("@iCertificateUserId", userId);


                url = Convert.ToString(dbo.GetScalar(sqlStr, pc.GetParams(true)));
                if (!String.IsNullOrEmpty(url))
                {
                    userInfo.userCards.WorkinfoCardUrl = url;
                    userInfo.cardPercent++;
                }
                //userCards
                int.TryParse(userId, out tmp);
                userInfo.userWorkingInfo = workingInfo;
                userInfo.userPersonalInfo = personalInfo;

                int allInfoNumber = workInfoNumber + personaNumber + contactNumber + cardNumber + otherInfoNumber;
                userInfo.allPercent = (userInfo.contactPercent + userInfo.personalPercent + userInfo.workingPercent + userInfo.cardPercent + userInfo.otherInfoPercent) * 100 / allInfoNumber;
                userInfo.workingPercent = userInfo.workingPercent * 100 / workInfoNumber;
                userInfo.personalPercent = userInfo.personalPercent * 100 / personaNumber;
                userInfo.contactPercent = userInfo.contactPercent * 100 / contactNumber;
                userInfo.cardPercent = userInfo.cardPercent * 100 / cardNumber;
                userInfo.otherInfoPercent = userInfo.otherInfoPercent * 100 / otherInfoNumber;
                result.result = Result.SUCCESS;
                result.data = userInfo;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = ex.Message;
                Log.WriteErrorLog("UserProvider::GetUserAllInfo", "获取用户所有信息：{0}|，异常：{1}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel UploadUserCertficate(int userId, string url, int type)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();

                string sqlStr = "select count(1) from IFCertificate where certificateType = @iCertificateType and certificateUserId = @iCertificateUserId";
                pc.Add("@iCertificateType", type);
                pc.Add("@iCertificateUserId", userId);

                int count = dbo.GetCount(sqlStr, pc.GetParams(true));
                Log.WriteDebugLog("UserProvider::UploadUserCertficate", "用户是否存在照片：{0}", count);
                if (count == 0)
                {
                    sqlStr = @"insert into IFCertificate(url, certificateType, CertificateUserId, createTime)
                                   values(@sUrl, @iCertificateType, @iCertificateUserId, now());";
                    pc.Add("@sUrl", url);
                    pc.Add("@iCertificateType", type);
                    pc.Add("@iCertificateUserId", userId);

                    int ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                    Log.WriteDebugLog("UserProvider::UploadUserCertficate", "用户不存在照片，插入({0})", ret);
                }
                else
                {
                    sqlStr = @"select count(1) from IFUserDebitRecord where userId = @iUserId and status in (0,1,2,4,-2,5,6)";

                    pc.Add("@iUserId", userId);
                    count = dbo.GetCount(sqlStr, pc.GetParams(true));
                    if (count > 0)
                    {
                        result.result = Result.ERROR;
                        result.message = "Tidak dapat mengubah informasi, karena Anda belum membayar kembali atau masih proses di dalam aplikasi.";
                        return result;
                    }
                    else
                    {
                        sqlStr = @"update IFCertificate set url = @sUrl, statusTime = now()
                                  where certificateType = @iCertificateType and certificateUserId = @iCertificateUserId";
                        pc.Add("@sUrl", url);
                        pc.Add("@iCertificateType", type);
                        pc.Add("@iCertificateUserId", userId);

                        int ret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                        Log.WriteDebugLog("UserProvider::UploadUserCertficate", "用户不存在贷款中记录，插入({0})", ret);
                    }
                }
                result.result = Result.SUCCESS;
                result.message = "success";
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.LOGIC_ERROR;
                result.message = "UserProvider::UploadUserCertficate logic is fail.";
                Log.WriteErrorLog("UserProvider::UploadUserCertficate", "获取失败：{0}|{1}，异常：{2}", userId, url, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel UpdateUserConactNumber(int userId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iUserId", userId);

                Hashtable table = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_contact_syncusernumber", pc.GetParams(), out table);
                if (null != dt && dt.Rows.Count == 1)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);

                    if (result.result < 0)
                    {
                        result.data = new { contactUploadNumber = 0, callRecordUploadNumber = 0 };
                        Log.WriteErrorLog("DebitProvider::UpdateUserConactNumber", "同步用户联系人数量异常");
                    }
                    else
                    {
                        result.result = Result.SUCCESS;
                        ///记录ID
                        result.data = new
                        {
                            contactUploadNumber = Convert.ToString(dt.Rows[0][1]),
                            callRecordUploadNumber = Convert.ToString(dt.Rows[0][2]),
                            location = Convert.ToString(dt.Rows[0][3])
                        };
                    }
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "error from the submit debit request.";
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.LOGIC_ERROR;
                result.message = "UserProvider::UpdateUserConactNumber logic is fail.";
                Log.WriteErrorLog("UserProvider::UpdateUserConactNumber", "获取失败：{0}，异常：{1}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <param name="callTime"></param>
        /// <param name="type"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static DataProviderResultModel UploadUserConacts(int userId, string name, string callTime, int type, string phone)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();

                string sqlStr = "select count(1) from IFUserContacts where userId = @iUserId and recordType = @iRecordType and phone = @sPhone;";
                pc.Add("@iUserId", userId);
                pc.Add("@iRecordType", type);
                pc.Add("@sPhone", phone);

                int count = dbo.GetCount(sqlStr, pc.GetParams(true));
                if (count == 0)
                {
                    sqlStr = @"insert into IFUserContacts(name,phone,callTime,recordType,createTime,userId)
                                   values(@sName, @sPhone, @sCallTime, @iRecordType,now(),@iUserId);";
                    pc.Add("@sName", name);
                    pc.Add("@sPhone", phone);
                    pc.Add("@sCallTime", callTime);
                    pc.Add("@iRecordType", type);
                    pc.Add("@iUserId", userId);

                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                }
                else
                {
                    sqlStr = @"update IFUserContacts set name = @sName, createTime = now()
                                  where userId = @iUserId and recordType = @iRecordType and phone = @sPhone;";
                    pc.Add("@sName", name);
                    pc.Add("@iUserId", userId);
                    pc.Add("@iRecordType", type);
                    pc.Add("@sPhone", phone);

                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                }
                result.result = Result.SUCCESS;
                result.message = "success";
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.LOGIC_ERROR;
                result.message = "UserProvider::UploadUserConacts logic is fail.";
                Log.WriteErrorLog("UserProvider::UploadUserConacts", "获取失败：{0}|{1}，异常：{2}", userId, phone, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel UploadUserConacts(int userId, int type, List<CallRecord> records)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();

                ParamCollections pc = new ParamCollections();

                string sqlStr = "select count(1) from IFUserDebitRecord where status in (0,1,2,4,-2,5,6) and userId = @iUserId";

                pc.Add("@iUserId", userId);
                int count = dbo.GetCount(sqlStr, pc.GetParams(true));

                if (count <= 0)
                {
                    sqlStr = "delete from IFUserContacts where userId = @iUserId and recordType = @iRecordType";
                    pc.Add("@iUserId", userId);
                    pc.Add("@iRecordType", type);
                    //pc.Add("@sPhone", records[i].phone);
                    dbo.ExecuteStatement(sqlStr, pc.GetParams(true));

                    sqlStr = String.Empty;
                    int submitNumber = 0;

                    if (type == 1)
                    {
                        for (int i = 0; i < records.Count; i++)
                        {
                            try
                            {
                                string strPhone = records[i].phone.Replace(" ", "");
                                strPhone = UserController.GetPhone(strPhone);

                                sqlStr += String.Format(@"insert into IFUserContacts(name,phone,callTime,recordType,createTime,userId,duration)
                                   values('{0}', '{1}', '{2}', {3},now(),{4}, {5});", records[i].name.Replace("'", ""), strPhone, records[i].callTime, type, userId, records[i].duration);

                                if (submitNumber++ > 30)
                                {
                                    dbo.ExecuteStatement(sqlStr, pc.GetParams());
                                    submitNumber = 0;
                                    sqlStr = String.Empty;
                                }
                            }
                            catch (Exception)
                            {
                                Log.WriteErrorLog("UserProvider::UploadUserConacts", "{0} 上传通讯录发生异常。记录数{1}", userId, records.Count);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < records.Count && i <=
                            700; i++)
                        {
                            try
                            {
                                string strPhone = records[i].phone.Replace(" ", "");
                                strPhone = UserController.GetPhone(strPhone);

                                sqlStr += String.Format(@"insert into IFUserContacts(name,phone,callTime,recordType,createTime,userId,duration)
                                   values('{0}', '{1}', '{2}', {3},now(),{4}, {5});", records[i].name.Replace("'", ""), strPhone, records[i].callTime, type, userId, records[i].duration);

                                if (submitNumber++ > 30)
                                {
                                    dbo.ExecuteStatement(sqlStr, pc.GetParams());
                                    submitNumber = 0;
                                    sqlStr = String.Empty;
                                }
                            }
                            catch (Exception)
                            {
                                Log.WriteErrorLog("UserProvider::UploadUserConacts", "{0} 上传通讯录发生异常。记录数{1}", userId, records.Count);
                            }
                        }
                    }
                    if (!String.IsNullOrEmpty(sqlStr))
                    {
                        dbo.ExecuteStatement(sqlStr, pc.GetParams());
                    }
                }
                result.result = Result.SUCCESS;
                result.message = "success";
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.LOGIC_ERROR;
                result.message = "UserProvider::UploadUserConacts logic is fail.";
                Log.WriteErrorLog("UserProvider::UploadUserConacts", "获取失败：{0}|{1}，异常：{2}", userId, type, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DataProviderResultModel UpdateUserLocation(UserLocationModel location)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"update IFUsers set locationX = @sLocationX,locationY =@sLocationY where userId = @iUserId;";
                pc.Add("@sLocationX", location.locationX);
                pc.Add("@sLocationY", location.locationY);
                pc.Add("@iUserId", location.userId);

                int count = dbo.ExecuteStatement(sqlStr, pc.GetParams());

                result.result = Result.SUCCESS;
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.LOGIC_ERROR;
                result.message = "UserProvider::UpdateUserLocation logic is fail.";
                Log.WriteErrorLog("UserProvider::UpdateUserLocation", "获取失败：{0}|{1}|{2}，异常：{3}", location.userId, location.locationX, location.locationY, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<UserCertificate> GetUserCertificate(int objId, int userId)
        {
            DataBaseOperator dbo = null;
            List<UserCertificate> infos = new List<UserCertificate>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select CertificateType, url, tableId, CertificateUserId from IFCertificate 
                                where tableId = @iObjectId and CertificateUserId = @iCertificateUserId";
                pc.Add("@iObjectId", objId);
                pc.Add("@iCertificateUserId", objId);

                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                if (null != dt && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        UserCertificate info = new UserCertificate();
                        info.resourceUrl = Convert.ToString(dt.Rows[i]["url"]);
                        info.objectId = objId;
                        info.userId = userId;
                        infos.Add(info);
                    }
                }
                return infos;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("UserProvider::GetUserCertificate", "获取失败：{0}|{1}，异常：{2}", userId, objId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return infos;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bankId"></param>
        /// <param name="userId"></param>
        /// <param name="bankName"></param>
        /// <param name="bankCode"></param>
        /// <param name="contactName"></param>
        /// <param name="contact"></param>
        /// <param name="subBankName"></param>
        /// <returns></returns>
        public static DataProviderResultModel SaveUserBankInfo(int bankId, string userId, string bankName, string bankCode, string contactName, string contact, string subBankName)
        {

            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserBankInfoModel bankInfo = new UserBankInfoModel();
            Redis redis = HelperProvider.GetRedis();
            try
            {
                if (redis.LockTake("SaveUserBankInfo", userId, 10))
                {
                    dbo = new DataBaseOperator();
                    ParamCollections pc = new ParamCollections();
                    pc.Add("@iBankId", bankId);
                    pc.Add("@iUserId", userId);
                    pc.Add("@sBankName", bankName);
                    pc.Add("@sSubBankName", subBankName);
                    pc.Add("@sBankCode", bankCode);
                    pc.Add("@sContact", contact);
                    pc.Add("@sContactName", contactName);

                    Hashtable table = new Hashtable();
                    DataTable dt = dbo.ExecProcedure("p_user_editbankinfo", pc.GetParams(), out table);
                    if (null != dt && dt.Rows.Count == 1)
                    {
                        int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                        if (result.result == Result.SUCCESS)
                        {
                            int.TryParse(Convert.ToString(dt.Rows[0][1]), out bankInfo.bankId);
                            bankInfo.bankName = bankName;
                            bankInfo.bankCode = bankCode;
                            bankInfo.contact = contact;
                            bankInfo.contactName = contactName;
                            bankInfo.subBankName = subBankName;
                        }
                        else
                        {
                            result.message = Convert.ToString(dt.Rows[0][2]);
                            Log.WriteErrorLog("UserProvider::SaveUserBankInfo", "{0}|{1}", result.result, dt.Rows[0][1]);
                            bankInfo.bankId = -1;
                        }
                    }
                    else
                    {
                        result.result = MainErrorModels.LOGIC_ERROR;
                        result.message = "The database logic error.The function is UserProvider::SaveUserBankInfo";
                    }
                    result.data = bankInfo;
                    redis.LockRelease("SaveUserBankInfo", userId);
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "Tet the lock error.The function is UserProvider::SaveUserBankInfo";
                    Log.WriteDebugLog("UserProvider::SaveUserBankInfo", result.message);
                }
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SaveUserBankInfo";
                Log.WriteErrorLog("UserProvider::SaveUserBankInfo", "获取失败：{0}，异常：{1}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel SaveUserBankInfoV2(UserBankInfoModel bankInfo)
        {

            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                pc.Add("@iBankId", bankInfo.bankId);
                pc.Add("@iUserId", bankInfo.userId);
                pc.Add("@sBankName", bankInfo.bankName);
                pc.Add("@sSubBankName", bankInfo.subBankName);
                pc.Add("@sBankCode", bankInfo.bankCode);
                pc.Add("@sContact", bankInfo.contact);
                pc.Add("@sContactName", bankInfo.contactName);
                pc.Add("@sBniCode", bankInfo.bniBankCode);

                Hashtable table = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_user_editbankinfo_v2", pc.GetParams(), out table);
                if (null != dt && dt.Rows.Count == 1)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    if (result.result == Result.SUCCESS)
                    {
                        int.TryParse(Convert.ToString(dt.Rows[0][1]), out bankInfo.bankId);
                    }
                    else
                    {
                        result.message = Convert.ToString(dt.Rows[0][2]);
                        Log.WriteErrorLog("UserProvider::SaveUserBankInfoV2", "{0}|{1}", result.result, dt.Rows[0][1]);
                        bankInfo.bankId = -1;
                    }
                }
                else
                {
                    result.result = MainErrorModels.LOGIC_ERROR;
                    result.message = "The database logic error.The function is UserProvider::SaveUserBankInfoV2";
                }
                result.data = bankInfo;
                return result;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SaveUserBankInfoV2";
                Log.WriteErrorLog("UserProvider::SaveUserBankInfoV2", "获取失败：{0}，异常：{1}", bankInfo.userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataProviderResultModel SaveUserWorkingInfo(UserWorkingInfoModel model)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = String.Empty;

                sqlStr = "select count(1) from IFUsers where userId = @iUserId";
                pc.Add("@iUserId", model.userId);
                int count = dbo.GetCount(sqlStr, pc.GetParams(true));

                if (count > 0)
                {
                    sqlStr = @"update IFUsers set typeOfWork = @iTypeOfWork,monthIncome = @dMonthIncome,companyName = @sCompanyName,
		companyProvince = @iCompanyProvince,companyCity = @iCompanyCity,companyDistrics = @iCompanyDistrics,
        companyDistricts = @iCompanyDistricts,companyAddress = @sCompanyAddress,companyPhone = @sCompanyPhone
        where userId = @iUserId;";
                    pc.Add("@iTypeOfWork", model.typeOfWork);
                    pc.Add("@dMonthIncome", model.monthIncome);
                    pc.Add("@sCompanyName", model.companyName);
                    pc.Add("@iCompanyProvince", model.companyProvince);
                    pc.Add("@iCompanyCity", model.companyCity);
                    pc.Add("@iCompanyDistrics", model.companyDistrics);
                    pc.Add("@iCompanyDistricts", model.companyDistricts);
                    pc.Add("@sCompanyAddress", model.address);
                    pc.Add("@sCompanyPhone", model.companyPhone);
                    pc.Add("@iUserId", model.userId);

                    dbo.ExecuteStatement(sqlStr, pc.GetParams());
                    result.result = Result.SUCCESS;
                }
                else
                {
                    result.result = MainErrorModels.THE_USER_NOT_EXISTS;
                    result.message = "The user not exists.";
                }

            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SaveUserContactInfo";
                Log.WriteErrorLog("UserProvider::SaveUserContactInfo", "获取失败：{0}|{1}，异常：{2}", model.userId, model.companyName, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel SaveUserPersonalInfo(UserPersonalInfoModel model)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = String.Empty;

                sqlStr = "select count(1) from IFUsers where userId = @iUserId";
                pc.Add("@iUserId", model.userId);
                int count = dbo.GetCount(sqlStr, pc.GetParams(true));

                if (count > 0)
                {
                    sqlStr = "SELECT count(1) FROM debit.IFUsers where idcard = @sIdcard and userId <> @iUserId";
                    pc.Add("@sIdcard", model.idCard);
                    pc.Add("@iUserId", model.userId);
                    count = dbo.GetCount(sqlStr, pc.GetParams(true));
                    if (count > 0)
                    {
                        result.result = MainErrorModels.IDCARD_ALREADY_USEED;
                        result.message = "The idcard already exists.";
                        return result;
                    }
                    sqlStr = @"update IFUsers set fullName = @sFullName,motherName = @sMotherName,socialAccounts = @sSocialAccounts,idcard = @sIdCard,sex = @iSex,
                        occupancyDuration = @iOccupancyDuration,education = @iEducation,numberOfChildren = @iNumberOfChildren,
                        residentialAddress = @sResidentialAddress, residentialDistricts = @iResidentialDistricts,
                        residentialDistrics = @iResidentialDistrics, residentialCity = @iResidentialCity,
                        residentialProvince = @iResidentialProvince, maritalStatus = @iMaritalStatus,birthday=@dBirthday
                        where userId = @iUserId;";
                    pc.Add("@sFullName", HttpUtility.UrlDecode(model.fullName));
                    pc.Add("@sMotherName", HttpUtility.UrlDecode(model.motherName));
                    pc.Add("@sSocialAccounts", model.socialAccounts);
                    pc.Add("@sIdCard", model.idCard);
                    pc.Add("@iSex", model.gender);
                    pc.Add("@iOccupancyDuration", model.occupancyDuration);
                    pc.Add("@iEducation", model.education);
                    pc.Add("@iNumberOfChildren", model.numberOfChildren);
                    pc.Add("@sResidentialAddress", HttpUtility.UrlDecode(model.address));
                    pc.Add("@iResidentialDistricts", model.residentialDistricts);
                    pc.Add("@iResidentialDistrics", model.residentialDistrics);
                    pc.Add("@iResidentialCity", model.residentialCity);
                    pc.Add("@iResidentialProvince", model.residentialProvince);
                    pc.Add("@iMaritalStatus", model.maritalStatus);
                    pc.Add("@dBirthday", HttpUtility.UrlDecode(model.birthday));
                    pc.Add("@iUserId", model.userId);

                    dbo.ExecuteStatement(sqlStr, pc.GetParams());
                    result.result = Result.SUCCESS;
                }
                else
                {
                    result.result = MainErrorModels.THE_USER_NOT_EXISTS;
                    result.message = "The user not exists.";
                }

            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SaveUserContactInfo";
                Log.WriteErrorLog("UserProvider::SaveUserContactInfo", "获取失败：{0}|{1}，异常：{2}", model.userId, model.fullName, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }


        /// <summary>
        /// 同步facebook的用户信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataProviderResultModel SyncFaceBookUserInfo(string userId, FaseBookUserInfo userInfo)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = String.Empty;
                sqlStr = @"update IFUsers set facebookId = @iFacebookId,facebookInfo= @sFacebookInfo where userId = @iUserId";

                pc.Add("@iFacebookId", userInfo.id);
                pc.Add("@sFacebookInfo", JsonConvert.SerializeObject(userInfo));
                pc.Add("@iUserId", userId);

                dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SyncFaceBookUserInfo";
                Log.WriteErrorLog("UserProvider::SyncFaceBookUserInfo", "获取失败：{0}|{1}，异常：{2}", userId, userInfo.id, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataProviderResultModel SyncUserRegistration(string userId, string registrationId)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = String.Empty;
                sqlStr = @"update IFUsers set registrationId = @iRegistrationId where userId = @iUserId";

                pc.Add("@iRegistrationId", registrationId);
                pc.Add("@iUserId", userId);

                dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SyncUserRegistration";
                Log.WriteErrorLog("UserProvider::SaveUserContactInfo", "获取失败：{0}|{1}，异常：{2}", userId, registrationId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel CheckStatusBeforModifyInfo(int userId)
        {

            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            UserInfoModel userInfo = new UserInfoModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string flag = String.Empty;
                pc.Add("@iUserId", userId);
                Hashtable outAl = new Hashtable();
                DataTable dt = dbo.ExecProcedure("p_check_status_befor_modify", pc.GetParams(), out outAl);

                string message = String.Empty;
                if (null != dt && dt.Rows.Count > 0)
                {
                    int.TryParse(Convert.ToString(dt.Rows[0][0]), out result.result);
                    result.message = Convert.ToString(dt.Rows[0][1]);
                }
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("UserProvider::CheckStatusBeforModifyInfo", "检查状态：{0}|，异常：{1}", userId, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataProviderResultModel SaveUserContactInfo(UserContactInfoModel model)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = String.Empty;

                string strPhone = model.phone.Replace(" ", "");
                strPhone = UserController.GetPhone(strPhone);

                if (model.id < 1)
                {
                    sqlStr = @"select count(1) from IFUserContactInfo 
                        where userId = @iUserId and relationShip = @iRelationShip";

                    pc.Add("@iUserId", model.userId);
                    pc.Add("@iRelationShip", model.relationShip);

                    int count = dbo.GetCount(sqlStr, pc.GetParams(true));
                    if (count > 0)
                    {
                        sqlStr = @"delete from IFUserContactInfo 
                        where userId = @iUserId and relationShip = @iRelationShip";

                        pc.Add("@iUserId", model.userId);
                        pc.Add("@iRelationShip", model.relationShip);
                        dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                    }

                    sqlStr = @"insert into IFUserContactInfo(userId, relationShip, relationUserName, phone, address)
    values(@iUserId, @iRelationShip, @sRelationUserName, @sPhone, @sAddress); ";

                    pc.Add("@iUserId", model.userId);
                    pc.Add("@iRelationShip", model.relationShip);
                    pc.Add("@sRelationUserName", model.relationUserName);
                    pc.Add("@sPhone", strPhone);
                    pc.Add("@sAddress", model.address);
                }
                else
                {
                    sqlStr = @"update IFUserContactInfo
set relationShip = @iRelationShip, relationUserName = @sRelationUserName, phone = @sPhone, address = @sAddress
where id = @iId;";
                    pc.Add("@iRelationShip", model.relationShip);
                    pc.Add("@sRelationUserName", model.relationUserName);
                    pc.Add("@sPhone", strPhone);
                    pc.Add("@sAddress", model.address);
                    pc.Add("@iId", model.id);
                }

                dbo.ExecuteStatement(sqlStr, pc.GetParams());
                result.result = Result.SUCCESS;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::SaveUserContactInfo";
                Log.WriteErrorLog("UserProvider::SaveUserContactInfo", "获取失败：{0}|{1}，异常：{2}", model.userId, model.relationUserName, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;
        }

        public static DataProviderResultModel CheckUserConactsInfo(CheckUserConactsRequestBodyModel model)
        {
            DataBaseOperator dbo = null;
            DataProviderResultModel result = new DataProviderResultModel();
            SortedList<string, int> list = new SortedList<string, int>();
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"select count(1) from IFUserContacts d 
                        where d.userId = @iUserId 
                        and d.phone = @sPhone and d.recordType = @iRecordType;";
                
                pc.Add("@iUserId", model.userId);
                pc.Add("@sPhone", model.phone);
                pc.Add("@iRecordType", 1);
                int count = dbo.GetCount(sqlStr, pc.GetParams());
                
                result.result = Result.SUCCESS;
                result.data = count;
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.The function is UserProvider::CheckUserConactsInfo";
                Log.WriteErrorLog("UserProvider::CheckUserConactsInfo", "获取失败：{0}{1}{2}，异常：{3}", model.userId, model.phone, model.relationShip, ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }
            return result;

        }
    }
}