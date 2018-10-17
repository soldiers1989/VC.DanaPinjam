using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NF.AdminSystem.Controllers
{
    [Route("api/Home")]
    public class HomeController : Controller
    {
        [AllowAnonymous]
        [Route("agreement")]
        public ActionResult agreement()
        {
            ViewBag.Title = "agreement";

            return View();
        }
        
        [AllowAnonymous]
        [Route("Privacy")]
        public ActionResult Privacy()
        {
            ViewBag.Title = "Privacy Policy";

            return View();
        }

        [AllowAnonymous]
        [Route("agreement2")]
        public ActionResult agreement2()
        {
            ViewBag.Title = "agreement";

            return View();
        }

        [AllowAnonymous]
        [Route("Index")]
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page 2.0";

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

            return View();
        }

        [AllowAnonymous]
        [Route("Help")]
        public ActionResult Help()
        {
            ViewBag.Title = "Help Page";

            return View();
        }

        [AllowAnonymous]
        [Route("Contactus")]
        public ActionResult Contactus()
        {
            ViewBag.Title = "Contactus Page";

            return View();
        }
    }
}
