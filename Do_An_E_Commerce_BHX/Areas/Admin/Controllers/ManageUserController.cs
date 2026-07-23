using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageUserController : Controller
    {
        // GET: Admin/ManageUser
        public ActionResult Index()
        {   
            return View();
        }
    }
}