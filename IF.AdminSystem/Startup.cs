using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using YYLog.ClassLibrary;
using DBMonoUtility;
using StackExchange.Redis;
using NF.AdminSystem;

namespace IF.AdminSystem
{
    public class Startup
    {
        public Startup(IConfiguration config)
        {
            Configuration = config;
            Log.Init(1, 10240000, "yyyyMMdd", @".", LogType.Debug);

            string DBName = config["AppSettings:DBName"];
            string publicKey = config["AppSettings:publicKey"];
            
            DataBaseOperator.SetDbIniFilePath(".");
            Log.WriteDebugLog("Startup::Startup", "Begin connect db");
            string conStr = DataBasePool.AddDataBaseConnectionString(DBName, publicKey, 5, 5);
            DataBaseOperator.Init(DBName);
            Log.WriteDebugLog("WebApiApplication::Application_Start", "数据库连接串：{0}", conStr);
 

            string serverInfo = config["AppSettings:RedisExchangeHosts"];
            string password = config["AppSettings:RedisExchangePwd"];
            RedisPools.RedisPools.Init(serverInfo, Proxy.None, 5, password);


        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(option => {
                option.Filters.Add(typeof(CustomActionFilterAttribute));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.Configure<AppSettingsModel>(Configuration.GetSection("AppSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}