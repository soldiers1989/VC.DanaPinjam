using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YYLog.ClassLibrary;
using Microsoft.AspNetCore.HostFiltering;
using NF.AdminSystem.Models;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Net.Http;
using Newtonsoft.Json;
using RedisPools;
using NF.AdminSystem.Providers;

namespace NF.AdminSystem
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NoFilterAttribute : Attribute { }

    public class CustomActionFilterAttribute: AuthorizeFilter
    {
        public override async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            string path = filterContext.HttpContext.Request.QueryString.ToString();
            var list = filterContext.ActionDescriptor.FilterDescriptors.Where(p=>((FilterDescriptor)p).Filter.GetType() == typeof(AllowAnonymousFilter));

            if (list.Count() == 1 || path.ToLower().IndexOf("isdebug") > -1)
            {
                return;
            }
            string token = filterContext.HttpContext.Request.Headers["token"];
            string userId = filterContext.HttpContext.Request.Headers["userId"];
            string qudao = filterContext.HttpContext.Request.Headers["qudao"];
            string version = filterContext.HttpContext.Request.Headers["version"];

            if (String.IsNullOrEmpty(token))
            {
                HttpResultModel ret = new HttpResultModel
                {
                    result = MainErrorModels.THE_TOKEN_VALIDATION_FAILED,
                    message = "Token validation failed."
                };
                //var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK);
                var content = new ContentResult();
                content.Content = JsonConvert.SerializeObject(ret);
                content.StatusCode = 200;
                filterContext.Result = content;

                Log.WriteWarning("CustomActionFilter::OnActionExecuting", "{0} {1} {2} {3} version:{4}", token, userId, qudao, ret.message, version);
            }
            else
            {
                /*
                Redis redis = HelperProvider.GetRedis();
                string guid = redis.StringGet(String.Format("user_guid_{0}", userId));
                
                string ctoken = HelperProvider.MD5Encrypt32(String.Format("{0}{1}", userId, guid));

                if (ctoken != token)
                {
                    HttpResultModel ret = new HttpResultModel
                    {
                        result = MainErrorModels.THE_TOKEN_VALIDATION_FAILED,
                        message = "Token validation failed."
                    };
                    var content = new ContentResult();
                    content.Content = JsonConvert.SerializeObject(ret);
                    content.StatusCode = 200;
                    filterContext.Result = content;

                    Log.WriteDebugLog("CustomActionFilter::OnActionExecuting", "{0} {1} {2} {3} version:{4}", token, userId, qudao, ctoken, version);
                }
                 */
            }

            //await base.OnAuthorizationAsync(filterContext);
         }
/*
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Log.WriteDebugLog("::OnActionExecuting", "{0}", filterContext.HttpContext.Request.Path);
            
            var list = filterContext.Filters.Where(p => (((IFilterMetadata)p).GetType() == typeof(NoFilterAttribute)));
            if (list.Count() == 1 || 
            filterContext.HttpContext.Request.Path.ToString().IndexOf("isdebug") > 0)
            {
                return;
            }
            
        } */
    }
}