using GCTL.Service.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Filters
{
    public enum PermissionType
    {
        Page,
        Add,
        Edit,
        Delete,
        Print
    }

    // ================================
    // ১. শুধু ControllerName দিয়ে চেক (পুরো controller এক module)
    // ================================
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ModulePermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly PermissionType permissionType;

        public ModulePermissionAttribute(PermissionType permissionType = PermissionType.Page)
        {
            this.permissionType = permissionType;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            //var accessCode = context.HttpContext.Session.GetString("AccessCode");
            var accessCode = context.HttpContext.User.FindFirst("AccessCode")?.Value;
            if (string.IsNullOrEmpty(accessCode))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();

            if (string.IsNullOrEmpty(controllerName))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            bool allowed = permissionType switch
            {
                PermissionType.Add => await permissionService.HasAddPermissionByControllerAsync(accessCode, controllerName),
                PermissionType.Edit => await permissionService.HasEditPermissionByControllerAsync(accessCode, controllerName),
                PermissionType.Delete => await permissionService.HasDeletePermissionByControllerAsync(accessCode, controllerName),
                PermissionType.Print => await permissionService.HasPrintPermissionByControllerAsync(accessCode, controllerName),
                _ => await permissionService.HasPagePermissionByControllerAsync(accessCode, controllerName)
            };

            if (!allowed)
            {
                if (IsAjaxRequest(context.HttpContext))
                {
                    context.Result = new JsonResult(new { success = false, message = "You do not have permission to perform this action." }) { StatusCode = 403 };
                }
                else
                {
                    var referer = context.HttpContext.Request.Headers["Referer"].ToString();

                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["PermissionDeniedMessage"] = "You do not have permission to access this page.";
                    }

                    if (!string.IsNullOrEmpty(referer))
                    {
                        context.Result = new RedirectResult(referer);
                    }
                    else
                    {
                        // Referer না পেলে fallback হিসেবে আগের পেজ বা হোমে পাঠান
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                    }
                }
                return;
            }

            await next();
        }

        private static bool IsAjaxRequest(HttpContext httpContext)
            => httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    // ================================
    // ২. ControllerName + ActionName দুটো মিলিয়ে চেক (একই controller-এ একাধিক module/menu হলে)
    // ================================
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ActionPermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly PermissionType permissionType;

        public ActionPermissionAttribute(PermissionType permissionType = PermissionType.Page)
        {
            this.permissionType = permissionType;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            //var accessCode = context.HttpContext.Session.GetString("AccessCode");

            var accessCode = context.HttpContext.User.FindFirst("AccessCode")?.Value;
            if (string.IsNullOrEmpty(accessCode))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            bool allowed = permissionType switch
            {
                PermissionType.Add => await permissionService.HasAddPermissionByControllerActionAsync(accessCode, controllerName, actionName),
                PermissionType.Edit => await permissionService.HasEditPermissionByControllerActionAsync(accessCode, controllerName, actionName),
                PermissionType.Delete => await permissionService.HasDeletePermissionByControllerActionAsync(accessCode, controllerName, actionName),
                PermissionType.Print => await permissionService.HasPrintPermissionByControllerActionAsync(accessCode, controllerName, actionName),
                _ => await permissionService.HasPagePermissionByControllerActionAsync(accessCode, controllerName, actionName)
            };

            if (!allowed)
            {
                if (IsAjaxRequest(context.HttpContext))
                {
                    context.Result = new JsonResult(new { success = false, message = "You do not have permission to perform this action." }) { StatusCode = 403 };
                }
                else
                {
                    var referer = context.HttpContext.Request.Headers["Referer"].ToString();

                    if (context.Controller is Controller controller)
                    {
                        controller.TempData["PermissionDeniedMessage"] = "You do not have permission to access this page.";
                    }

                    if (!string.IsNullOrEmpty(referer))
                    {
                        context.Result = new RedirectResult(referer);
                    }
                    else
                    {
                        // Referer না পেলে fallback হিসেবে আগের পেজ বা হোমে পাঠান
                        context.Result = new RedirectToActionResult("Index", "Home", null);
                    }
                }
                return;
            }

            await next();
        }

        private static bool IsAjaxRequest(HttpContext httpContext)
            => httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}