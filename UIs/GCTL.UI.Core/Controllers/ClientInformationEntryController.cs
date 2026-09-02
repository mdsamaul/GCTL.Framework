using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.ClientInformationEntry;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortName;
using GCTL.Data.Models;
using GCTL.Service.ClientInformationEntry;
using GCTL.Service.Common;
using GCTL.Service.PortName;
using GCTL.UI.Core.ViewModels.ClientInformationEntry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class ClientInformationEntryController : BaseController
    {
        #region Service & Repository
        public readonly IClientInformationEntryService clientInformationEntryService;
        private readonly IRepository<CaDefCountry> countryRepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public ClientInformationEntryController(
            IClientInformationEntryService clientInformationEntryService,
            IRepository<CaDefCountry> countryRepository,
            ICommonService commonService

            )
        {
            this.clientInformationEntryService = clientInformationEntryService;
            this.countryRepository = countryRepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await clientInformationEntryService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ClientInformationEntryPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await clientInformationEntryService.GetAllAsync();
                model.ClientInformationEntryList = list ?? new List<ClientInformationEntrySetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "ClientID", "HRM_ClientInfo", 4);

                ViewBag.CountryDD = new SelectList(countryRepository.All(), "CountryId", "CountryName");

                model.Setup = new ClientInformationEntrySetupViewModel
                {
                    ClientId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.ClientInformationEntryList = new List<ClientInformationEntrySetupViewModel>();
                model.Setup = new ClientInformationEntrySetupViewModel();
                Console.WriteLine("Error" + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            ClientInformationEntrySetupViewModel model = new ClientInformationEntrySetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "ClientID", "HRM_ClientInfo", 4);

            ViewBag.CountryDD = new SelectList(countryRepository.All(), "CountryId", "CountryName");

            if (!string.IsNullOrEmpty(id))
            {
                model = await clientInformationEntryService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.ClientId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(ClientInformationEntrySetupViewModel modelVM)
        {
            try
            {
                if (await clientInformationEntryService.IsExistAsync(modelVM.ClientId, modelVM.ClientName))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                modelVM.UserCompany = LoginInfo.CompanyCode;
                if (modelVM.AutoId == 0)
                {
                    var hasSavePermission = await clientInformationEntryService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await clientInformationEntryService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.ClientId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await clientInformationEntryService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await clientInformationEntryService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.ClientId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                return RedirectToAction("Login", "Accounts");

            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { success = false, message = "No IDs provided for delete." });
            }

            var hasPermission = await clientInformationEntryService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await clientInformationEntryService.DeleteTab(ids, model);
            if (success.succses)
            {
                return Json(new { success = true, message = "Deleted Successfully." });
            }
            else
            {
                return Json(new { success = false, message = success.messege });
            }
        }

        #endregion

        #region TabeleLodaing

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await clientInformationEntryService.GetAllAsync() ?? new List<ClientInformationEntrySetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<ClientInformationEntrySetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region Chake Degian

        //public IActionResult Index()
        //{
        //    ClientInformationEntryPageViewModel model = new ClientInformationEntryPageViewModel
        //    {
        //        Setup = new ClientInformationEntrySetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
