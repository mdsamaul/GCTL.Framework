using GCTL.Core.ViewModels;
using GCTL.Service.MenuBreadcrumbs;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.ViewComponents
{
    public class BreadcrumbViewComponent : ViewComponent
    {
        private readonly IMenuBreadcrumbService _breadcrumbService;

        public BreadcrumbViewComponent(IMenuBreadcrumbService breadcrumbService) => _breadcrumbService = breadcrumbService;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (ViewData["CustomBreadcrumb"] is List<BreadcrumbItem> customTrail && customTrail.Any())
                return View(customTrail);

            var controller = ViewContext.RouteData.Values["controller"]?.ToString();
            var action = ViewContext.RouteData.Values["action"]?.ToString();
            var overrideTitle = ViewData["Title"] as string;

            var trail = await _breadcrumbService.BuildBreadcrumbAsync(controller, action, overrideTitle);
            return View(trail);
        }
    }
}
