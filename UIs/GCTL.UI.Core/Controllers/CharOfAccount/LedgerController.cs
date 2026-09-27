
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class LedgerController : BaseController
    {
        public LedgerController() : base()
        {

        }

        public IActionResult Index()
        {

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView();
            }
            return View();
        }



    }
}
