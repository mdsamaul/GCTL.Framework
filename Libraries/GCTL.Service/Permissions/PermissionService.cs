using GCTL.Core.Data;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GCTL.Service.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly IRepository<CoreAccessCode> accessCodeRepository;

        public PermissionService(IRepository<CoreAccessCode> accessCodeRepository)
        {
            this.accessCodeRepository = accessCodeRepository;
        }

        // ---------- Controller-only ----------
        public async Task<bool> HasPagePermissionByControllerAsync(string accessCode, string controllerName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.TitleCheck);

        public async Task<bool> HasAddPermissionByControllerAsync(string accessCode, string controllerName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.CheckAdd);

        public async Task<bool> HasEditPermissionByControllerAsync(string accessCode, string controllerName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.CheckEdit);

        public async Task<bool> HasDeletePermissionByControllerAsync(string accessCode, string controllerName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.CheckDelete);

        public async Task<bool> HasPrintPermissionByControllerAsync(string accessCode, string controllerName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.CheckPrint);

        // ---------- Controller + Action ----------
        public async Task<bool> HasPagePermissionByControllerActionAsync(string accessCode, string controllerName, string actionName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.ViewName == actionName && x.TitleCheck);

        public async Task<bool> HasAddPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.ViewName == actionName && x.CheckAdd);

        public async Task<bool> HasEditPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.ViewName == actionName && x.CheckEdit);

        public async Task<bool> HasDeletePermissionByControllerActionAsync(string accessCode, string controllerName, string actionName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.ViewName == actionName && x.CheckDelete);

        public async Task<bool> HasPrintPermissionByControllerActionAsync(string accessCode, string controllerName, string actionName)
            => await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.ControllerName == controllerName && x.ViewName == actionName && x.CheckPrint);

        // ---------- Add OR Edit (একই action, id দেখে decide) ----------
        public async Task<bool> HasAddOrEditPermissionByControllerAsync(string accessCode, string controllerName, bool isNewRecord)
        {
            if (isNewRecord)
                return await HasAddPermissionByControllerAsync(accessCode, controllerName);

            return await HasEditPermissionByControllerAsync(accessCode, controllerName);
        }
    }
}