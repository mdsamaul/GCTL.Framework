
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
            return View();
        }



    }
}
