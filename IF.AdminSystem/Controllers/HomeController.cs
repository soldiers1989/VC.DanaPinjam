using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NF.AdminSystem.Controllers
{
    [Route("api/Home")]
    public class HomeController : Controller
    {
        private AppSettingsModel ConfigSettings { get; set; }
        public HomeController(IOptions<AppSettingsModel> settings)
        {
            ConfigSettings = settings.Value;
        }
        [AllowAnonymous]
        [Route("agreement")]
        public ActionResult agreement()
        {
            ViewBag.Title = "agreement";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }

        [AllowAnonymous]
        [Route("agreement2")]
        public ActionResult agreement2()
        {
            ViewBag.Title = "agreement";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }

        [AllowAnonymous]
        [Route("Index")]
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }

        [AllowAnonymous]
        [Route("info")]
        public ActionResult info()
        {
            ViewBag.Title = "Info";

            return View();
        }

        [AllowAnonymous]
        [Route("About")]
        public ActionResult About()
        {
            ViewBag.Title = "About Page";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }

        [AllowAnonymous]
        [Route("Help")]
        public ActionResult Help()
        {
            ViewBag.Title = "Help Page";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }

        [AllowAnonymous]
        [Route("Contactus")]
        public ActionResult Contactus()
        {
            ViewBag.Title = "Contactus Page";
            ViewData["platform"] = ConfigSettings.platform;

            return View();
        }
    }
}
