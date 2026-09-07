using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.CustomerType;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Service.Common;
using GCTL.Service.CustomerType;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.CustomerType;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerTypeController : BaseController
    {
        #region Declarations
        private readonly ICustomerTypeService _customerTypeService;
        private readonly ICommonService _commonService;
        private string strMaxNO = string.Empty;

        public CustomerTypeController(
            ICustomerTypeService customerTypeService,
            ICommonService commonService
            )
        {
            _customerTypeService = customerTypeService;
            _commonService = commonService;
        }

        #endregion

        #region Get Next Code

        private async Task<string> GetNextCodeAsync()
        {
            string strMaxNo = string.Empty;
            try
            {
                _commonService.FindMaxNo(ref strMaxNo, "CustomerTypeID", "Sales_Def_CustomerType", 3);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(strMaxNo) || strMaxNo == "000")
            {
                var list = await _customerTypeService.GetAllAsync();
                if (list != null && list.Any())
                {
                    var maxVal = list.Select(x => int.TryParse(x.CustomerTypeId, out int v) ? v : 0).Max();
                    strMaxNo = (maxVal + 1).ToString("D3");
                }
                else
                {
                    strMaxNo = "001";
                }
            }
            return strMaxNo;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await _customerTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new CustomerTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await _customerTypeService.GetAllAsync() ?? new List<CustomerTypeSetupViewModel>();
                model.CustomerTypeList = list;

                model.Setup = new CustomerTypeSetupViewModel
                {
                    CustomerTypeId = await GetNextCodeAsync()
                };
            }
            catch (Exception ex)
            {
                model.CustomerTypeList = new List<CustomerTypeSetupViewModel>();
                model.Setup = new CustomerTypeSetupViewModel();
                Console.WriteLine("Error: " + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            var model = new CustomerTypeSetupViewModel();
            var nextCode = await GetNextCodeAsync();

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await _customerTypeService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.CustomerTypeId = nextCode;
                }
            }
            else
            {
                model.CustomerTypeId = nextCode;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new CustomerTypePageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await _customerTypeService.GetAllAsync() ?? new List<CustomerTypeSetupViewModel>();

            return View("Index", pageModel);
        }

        #endregion

        #region Save/Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CustomerTypeSetupViewModel modelVM)
        {
            try
            {
                if (await _customerTypeService.IsExistAsync(modelVM.CustomerType, modelVM.CustomerTypeId))
                {
                    return Json(new { isSuccess = false, message = "Customer Type Name already exists.", isDuplicate = true });
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, (modelVM.AutoId > 0 || modelVM.AutoId > 0));

                if (modelVM.AutoId == 0 || modelVM.AutoId == null || modelVM.AutoId == 0)
                {
                    var hasSavePermission = await _customerTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Save." });
                    }

                    var nextCode = await GetNextCodeAsync();
                    modelVM.CustomerTypeId = nextCode;

                    bool result = await _customerTypeService.SaveAsync(modelVM);
                    if (result)
                    {
                        var newNextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = newNextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Customer Type." });
                }
                else
                {
                    var hasUpdatePermission = await _customerTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no permission to Update." });
                    }

                    bool result = await _customerTypeService.UpdateAsync(modelVM);
                    if (result)
                    {
                        var nextCode = await GetNextCodeAsync();
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Customer Type." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { isSuccess = false, message = "An error occurred while saving." });
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { isSuccess = false, message = "No records selected to delete." });
            }

            var hasPermission = await _customerTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { isSuccess = false, message = "You have no permission to Delete." });
            }

            var deleteHistory = new DeleteHistoryViewModel();
            deleteHistory.ToAudit(LoginInfo);
            deleteHistory.CompanyCode = LoginInfo.CompanyCode;

            var result = await _customerTypeService.DeleteTab(ids, deleteHistory);
            return Json(new { isSuccess = result.succses, message = result.messege });
        }

        #endregion

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _customerTypeService.GetAllAsync() ?? new List<CustomerTypeSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<CustomerTypeSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region Chake Degian

        //public IActionResult Index()
        //{
        //    CustomerTypePageViewModel model = new CustomerTypePageViewModel
        //    {
        //        Setup = new CustomerTypeSetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
