using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NF.AdminSystem.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult agreement()
        {
            ViewBag.Title = "agreement";

            return View();
        }

        [AllowAnonymous]
        public ActionResult agreement2()
        {
            ViewBag.Title = "agreement";

            return View();
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            ViewBag.Title = "About Page";

            return View();
        }

        [AllowAnonymous]
        public ActionResult Help()
        {
            ViewBag.Title = "Help Page";

            return View();
        }
        
        [AllowAnonymous]
        public ActionResult Contactus()
        {
            ViewBag.Title = "Contactus Page";

            return View();
        }
    }
}
