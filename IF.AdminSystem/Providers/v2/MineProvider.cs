using DBMonoUtility;
using Newtonsoft.Json;
using NF.AdminSystem.Models;
using NF.AdminSystem.Models.v2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using YYLog.ClassLibrary;

namespace NF.AdminSystem.Providers.v2
{
    public class MineProvider
    {
        public static DataProviderResultModel GetUserQuestions(UserInfoRequestBody userInfo)
        {
            DataProviderResultModel result = new DataProviderResultModel();
            DataBaseOperator dbo = null;
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = "select id,userId, content, url, feedback, adminId, createTime, replyTime from IFUserQuestions where userId = @iUserId";

                pc.Add("@iUserId", userInfo.userId);
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams(true));
                List<UserQuestions> list = new List<UserQuestions>();
                if (null != dt)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        UserQuestions questions = new UserQuestions();
                        questions.userId = Convert.ToString(dt.Rows[i]["userId"]);
                        questions.id = Convert.ToString(dt.Rows[i]["id"]);
                        questions.content = Convert.ToString(dt.Rows[i]["content"]);
                        questions.feedback = Convert.ToString(dt.Rows[i]["feedback"]);
                        questions.createTime = Convert.ToString(dt.Rows[i]["createTime"]);
                        questions.replyTime = Convert.ToString(dt.Rows[i]["replyTime"]);
                        questions.url = Convert.ToString(dt.Rows[i]["url"]);
                        list.Add(questions);
                    }
                    result.data = list;
                    result.result = Result.SUCCESS;
                }
                else
                { 
                    result.result = Result.ERROR;
                    result.message = "The database logic error.Return data is null";
                }

            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("v2::MineProvider::GetUserQuestions", "Error：{0}, {1}", JsonConvert.SerializeObject(userInfo), ex.Message);
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

        public static DataProviderResultModel PostUserQuestions(QuestionsRequestBody requestBody)
        {
            DataProviderResultModel result = new DataProviderResultModel();
            DataBaseOperator dbo = null;
            try
            {
                dbo = new DataBaseOperator();
                ParamCollections pc = new ParamCollections();
                string sqlStr = @"insert into IFUserQuestions(userId, content, url, feedback, createTime)
                        values(@iUserId, @sContent, @sUrl, '', now());";

                pc.Add("@iUserId", requestBody.userId);
                pc.Add("@sContent", requestBody.content);
                pc.Add("@sUrl", requestBody.url);
                int dbret = dbo.ExecuteStatement(sqlStr, pc.GetParams(true));
                if (dbret > 0)
                {
                    result.result = Result.SUCCESS;
                }
                else
                { 
                    result.result = Result.ERROR;
                    result.message = "The database logic error.Return data is null";
                }
            }
            catch (Exception ex)
            {
                result.result = MainErrorModels.DATABASE_REQUEST_ERROR;
                result.message = "The database logic error.";
                Log.WriteErrorLog("v2::MineProvider::PostUserQuestions", "Error：{0}, {1}", JsonConvert.SerializeObject(requestBody), ex.Message);
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
