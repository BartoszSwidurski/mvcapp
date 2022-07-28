using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MvcApp.Controllers
{
    public class BaseController : Controller
    {
        private const string languageKey = "language";
        public BaseController()
        {

        }

        public IActionResult ChangeGlobalization([FromQuery] string language = "pl")
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(language);

            HttpContext.Session.SetString("language", language);

            return RedirectToAction("Index", "Home");
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var languageFromSession = HttpContext.Session.GetString(languageKey);

            base.OnActionExecuting(filterContext);
            var language = "pl";
            CultureInfo cultureInfo;
            if (languageFromSession == null)
            {
                cultureInfo = CultureInfo.GetCultureInfo(language);
            }
            else
            {
                language = languageFromSession;
                cultureInfo = CultureInfo.GetCultureInfo(language);
            }

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            ViewData["language"] = language;


        }


    }
}
