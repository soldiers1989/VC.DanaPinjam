using DBMonoUtility;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YYLog.ClassLibrary;

using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace NF.AdminSystem.Controllers
{
    public class AuditController : Controller
    {
        [AllowAnonymous]
        // GET: Audit
        public ActionResult DebitRecords(string userId, int status, string beginTime, string endTime)
        {
            var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add("DebitRecords");

            DataBaseOperator dbo = null;
            string result = String.Empty;
            try
            {
                if (String.IsNullOrEmpty(beginTime) || String.IsNullOrEmpty(endTime))
                {
                    beginTime = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    endTime = DateTime.Now.ToString("yyyy-MM-dd");
                }
                dbo = new DataBaseOperator();
                string sqlStr = String.Format(@"select debitId,userId, debitMoney, Status, createTime, description, bankId
, certificate, debitPeroid, payBackMoney,(select b.Description from IFUserAduitDebitRecord b where b.debitId = a.DebitId) auditInfo,
(select if(a.Status = 4, overdueDayInterest,b.interestRate)*a.DebitMoney from IFDebitStyle b where b.money = a.DebitMoney and b.period = a.DebitPeroid) dayInterset
                    from IFUserDebitRecord a where a.createTime >='{0}' and a.createTime < '{1}' ", beginTime, endTime);

                ParamCollections pc = new ParamCollections();
                if (!String.IsNullOrEmpty(userId))
                {
                    sqlStr += " and a.userId = @iUserId";
                    pc.Add("@iUserId", userId);
                }

                if (status != -1)
                {
                    sqlStr += " and a.status = @iStatus";
                    pc.Add("@iStatus", status);
                }
                DataTable dt = dbo.GetTable(sqlStr, pc.GetParams());

                ViewData["data"] = dt;

                ViewData["beginTime"] = beginTime;
                ViewData["endTime"] = endTime;
                ViewData["userId"] = userId;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("AuditController::DebitRecords", ex.Message);
            }
            finally
            {
                if (null != dbo)
                {
                    dbo.Close();
                    dbo = null;
                }
            }

            return View();
        }
    }
}