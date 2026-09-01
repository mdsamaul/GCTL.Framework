using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.CfDefPlaceOfLoading;
using GCTL.Service.Common;
using GCTL.Service.CfDefPlaceOfLoading;
using GCTL.UI.Core.ViewModels.CfDefPlaceOfLoading;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class CfDefPlaceOfLoadingController : BaseController
    {
        #region Service

        private readonly ICfDefPlaceOfLoadingService placeOfLoadingService;
        private readonly ICommonService commonService;

        private string strMaxNO = string.Empty;

        public CfDefPlaceOfLoadingController(
            ICfDefPlaceOfLoadingService placeOfLoadingService,
            ICommonService commonService)
        {
            this.placeOfLoadingService = placeOfLoadingService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            try
            {
                var hasPermission = await placeOfLoadingService.PagePermissionAsync(LoginInfo.AccessCode);

                if (hasPermission)
                {
                    return RedirectToAction("Login", "Accounts");
                }

                var model = new CfDefPlaceOfLoadingPageViewModel
                {
                    PageUrl = Url.Action(nameof(Index))
                };

                var list = await placeOfLoadingService.GetAllAsync();
                model.PlaceOfLoadingList = list ?? new List<CfDefPlaceOfLoadingSetupViewModel>();

                commonService.FindMaxNo(
                    ref strMaxNO,
                    "PlaceOfLoadingID",
                    "CF_Def_PlaceOfLoading",
                    3);

                model.Setup = new CfDefPlaceOfLoadingSetupViewModel
                {
                    PlaceOfLoadingId = strMaxNO
                };

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView(model);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in Index: {ex.Message}");

                return RedirectToAction("Login", "Accounts");
            }
        }

        #endregion

        #region Setup(GET)
        [HttpGet]
        public async Task<IActionResult> Setup(string id)
        {
            try
            {
                var model = new CfDefPlaceOfLoadingSetupViewModel();

                commonService.FindMaxNo(
                    ref strMaxNO,
                    "PlaceOfLoadingID",
                    "CF_Def_PlaceOfLoading",
                    3);

                if (!string.IsNullOrEmpty(id))
                {
                    model = await placeOfLoadingService.GetByIdAsync(id);

                    if (model == null)
                        return NotFound();
                }
                else
                {
                    model.PlaceOfLoadingId = strMaxNO;
                }

                return PartialView($"_{nameof(Setup)}", model);
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in Setup GET: {ex.Message}");
                return BadRequest("An error occurred while loading the form.");
            }
        }

        #endregion

        #region Setup(POST)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CfDefPlaceOfLoadingSetupViewModel model)
        {
            try
            {
                // Validate input
                if (model == null)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Invalid data provided."
                    });
                }

                // Trim whitespace from input
                if (!string.IsNullOrEmpty(model.PlaceOfLoading))
                {
                    model.PlaceOfLoading = model.PlaceOfLoading.Trim();
                }

                if (!string.IsNullOrEmpty(model.ShortName))
                {
                    model.ShortName = model.ShortName.Trim();
                }

                // Check for duplicate before validation
                if (await placeOfLoadingService.IsExistAsync(model.PlaceOfLoading, model.PlaceOfLoadingId))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        isDuplicate = true,
                        message = "Place of Loading name already exists!"
                    });
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var error = ModelState.Values
                        .SelectMany(x => x.Errors)
                        .FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

                    return Json(new
                    {
                        isSuccess = false,
                        message = error
                    });
                }

                // Apply audit information
                model.ToAudit(LoginInfo, model.Tc > 0);

                // Insert new record
                if (model.Tc == null || model.Tc == 0)
                {
                    var savePermission = await placeOfLoadingService.SavePermissionAsync(LoginInfo.AccessCode);

                    if (!savePermission)
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            noSavePermission = true,
                            message = "You do not have permission to save."
                        });
                    }

                    var saveResult = await placeOfLoadingService.SaveAsync(model);

                    if (saveResult)
                    {
                        return Json(new
                        {
                            isSuccess = true,
                            lastCode = model.PlaceOfLoadingId,
                            message = "Saved successfully."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = "Failed to save. Please try again."
                        });
                    }
                }
                // Update existing record
                else
                {
                    var updatePermission = await placeOfLoadingService.UpdatePermissionAsync(LoginInfo.AccessCode);

                    if (!updatePermission)
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            noUpdatePermission = true,
                            message = "You do not have permission to update."
                        });
                    }

                    var updateResult = await placeOfLoadingService.UpdateAsync(model);

                    if (updateResult)
                    {
                        return Json(new
                        {
                            isSuccess = true,
                            lastCode = model.PlaceOfLoadingId,
                            message = "Updated successfully."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = "Failed to update. Please try again."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in Setup POST: {ex.Message}");

                return Json(new
                {
                    isSuccess = false,
                    message = "An error occurred while processing your request."
                });
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No items selected for deletion."
                    });
                }

                var permission = await placeOfLoadingService.DeletePermissionAsync(LoginInfo.AccessCode);

                if (!permission)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You do not have permission to delete."
                    });
                }

                var deleteHistory = new DeleteHistoryViewModel();
                deleteHistory.ToAudit(LoginInfo);
                deleteHistory.CompanyCode = LoginInfo.CompanyCode;

                var result = await placeOfLoadingService.DeleteTab(ids, deleteHistory);

                if (result.succses)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Deleted successfully."
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.messege ?? "Failed to delete. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in Delete: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting."
                });
            }
        }

        #endregion

        #region Grid

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await placeOfLoadingService.GetAllAsync();

                if (list == null)
                {
                    list = new List<CfDefPlaceOfLoadingSetupViewModel>();
                }

                return Json(list);
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in GetTableData: {ex.Message}");

                return Json(new List<CfDefPlaceOfLoadingSetupViewModel>());
            }
        }

        #endregion

        #region Duplicate Check

        [HttpPost]
        public async Task<IActionResult> CheckAvailability(string code, string name)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = "Code and Name are required."
                    });
                }

                var exists = await placeOfLoadingService.IsExistAsync(name.Trim(), code.Trim());

                return Json(new
                {
                    isSuccess = exists,
                    message = exists ? "This Place of Loading name already exists." : ""
                });
            }
            catch (Exception ex)
            {
                // Log exception here
                Console.WriteLine($"Error in CheckAvailability: {ex.Message}");

                return Json(new
                {
                    isSuccess = false,
                    message = "An error occurred while checking availability."
                });
            }
        }

        #endregion
    }
}