using GCTL.Service.MenuBreadcrumbs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GCTL.UI.Core.Filters
{
    public class BreadcrumbActionFilter : IAsyncActionFilter
    {
        private readonly IMenuBreadcrumbService breadcrumbService;

        public BreadcrumbActionFilter(IMenuBreadcrumbService breadcrumbService)
        {
            this.breadcrumbService = breadcrumbService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            if (executedContext.Result is ViewResult viewResult)
            {
                if (string.IsNullOrWhiteSpace(viewResult.ViewData["Title"] as string))
                {
                    var controllerName = context.RouteData.Values["controller"]?.ToString();
                    var viewName = viewResult.ViewName ?? context.RouteData.Values["action"]?.ToString();

                    var trail = await breadcrumbService.BuildBreadcrumbAsync(controllerName, viewName, null);
                    var title = trail?.LastOrDefault(x => x.IsActive)?.Title ?? trail?.LastOrDefault()?.Title;

                    if (!string.IsNullOrWhiteSpace(title))
                        viewResult.ViewData["Title"] = title;
                }
            }
        }
    }
}
