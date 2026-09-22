using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class DocumentationReportController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
