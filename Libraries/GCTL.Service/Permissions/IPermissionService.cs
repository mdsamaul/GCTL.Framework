using System.Threading.Tasks;

namespace GCTL.Service.Permissions
{
    public interface IPermissionService
    {
        // Controller-only ভিত্তিক
        Task<bool> HasPagePermissionByControllerAsync(string accessCode, string controllerName);
        Task<bool> HasAddPermissionByControllerAsync(string accessCode, string controllerName);
        Task<bool> HasEditPermissionByControllerAsync(string accessCode, string controllerName);
        Task<bool> HasDeletePermissionByControllerAsync(string accessCode, string controllerName);
        Task<bool> HasPrintPermissionByControllerAsync(string accessCode, string controllerName);

        // Controller + Action ভিত্তিক
        Task<bool> HasPagePermissionByControllerActionAsync(string accessCode, string controllerName, string actionName);
        Task<bool> HasAddPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName);
        Task<bool> HasEditPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName);
        Task<bool> HasDeletePermissionByControllerActionAsync(string accessCode, string controllerName, string actionName);
        Task<bool> HasPrintPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName);

        // নতুন — একই action দিয়ে Create + Update দুটোই হ্যান্ডেল করার জন্য
        Task<bool> HasAddOrEditPermissionByControllerAsync(string accessCode, string controllerName, bool isNewRecord);
    }
}