using GCTL.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.MenuBreadcrumbs
{
    public interface IMenuBreadcrumbService
    {
        Task<List<BreadcrumbItem>> BuildBreadcrumbAsync(string controllerName, string viewName, string fallbackTitle);
        Task<List<BreadcrumbItem>> BuildBreadcrumbByMenuIdAsync(string menuId, string fallbackTitle);
        void InvalidateCache();
    }
}
