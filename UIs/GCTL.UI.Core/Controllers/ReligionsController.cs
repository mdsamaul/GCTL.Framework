using AutoMapper;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Religions;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.Designations;
using GCTL.Service.Religions;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Religions;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class ReligionsController : BaseController
    {
        private readonly IReligionService religionService;
        private readonly ICommonService commonService;
        private readonly IMapper mapper;
        string strMaxNO = "";
        public ReligionsController(IReligionService religionService,
                                   ICommonService commonService,
                                   IMapper mapper)
        {
            this.religionService = religionService;
            this.commonService = commonService;
            this.mapper = mapper;
        }

        public IActionResult Index(bool child = false)
        {
            ReligionPageViewModel model = new ReligionPageViewModel()
            {
                PageUrl = Url.Action(nameof(Index))
            };
            commonService.FindMaxNo(ref strMaxNO, "ReligionCode", "HRM_Def_Religion", 1);
            model.Setup = new ReligionSetupViewModel
            {
                ReligionCode = strMaxNO
            };

            if (child)
                return PartialView(model);

            return View(model);
        }


        public IActionResult Setup(string id)
        {
            ReligionSetupViewModel model = new ReligionSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "ReligionCode", "HRM_Def_Religion", 1);
            var result = religionService.GetReligion(id);
            if (result != null)
            {
                model = mapper.Map<ReligionSetupViewModel>(result);
                model.Code = id;
                model.Id = (int)result.AutoId;
            }
            else
            {
                model.ReligionCode = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Setup(ReligionSetupViewModel model)
        {
            if (religionService.IsReligionExist(model.Religion, model.ReligionCode))
            {
                return Json(new { isSuccess = false, message = "Already Exists" });
            }

            if (ModelState.IsValid)
            {
                if (religionService.IsReligionExistByCode(model.ReligionCode))
                {
                    var hasPermission = religionService.UpdatePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        HrmDefReligion religion = religionService.GetReligion(model.ReligionCode) ?? new HrmDefReligion();
                      
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, religion);
                        religion.ShortName = model.ShortName ?? string.Empty;
                        religionService.SaveReligion(religion);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = religion.ReligionCode });
                    }
                    else
                    {

                        return Json(new { isSuccess = false, message = "You have no access" });
                    }

                }
                else
                {
                    var hasPermission = religionService.SavePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        HrmDefReligion religion = religionService.GetReligion(model.ReligionCode) ?? new HrmDefReligion();
                      
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, religion);
                        religion.ShortName = model.ShortName ?? string.Empty;
                        religionService.SaveReligion(religion);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = religion.ReligionCode });
                    }
                    else
                    {

                        return Json(new { isSuccess = false, message = "You have no access" });
                    }
                }
              
            }

            return Json(new { success = false, message = ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage });
        }

        public ActionResult Grid()
        {
            var resutl = religionService.GetReligions();
            return Json(new { data = resutl });
        }

        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            var hasPermission = religionService.DeletePermission(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access" });
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { success = false, message = "No IDs provided" });
            }

            var results = new List<(bool success, bool refSuccess, string message)>();
            var ids = id.Split(",", StringSplitOptions.RemoveEmptyEntries)
                 .Select(x => x.Trim())
                 .ToList();

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var result = await religionService.DeleteReligion(ids, model);

            results.Add(result);


            // Aggregate outcome
            bool overallSuccess = results.All(r => r.success);
            bool refSuccess = results.All(r => r.refSuccess);
            string combinedMessage = overallSuccess
                ? "Deleted Successfully"
                : string.Join("; ", results.Where(r => !r.success).Select(r => r.message));

            return Json(new { success = overallSuccess, message = combinedMessage, refSuccess = refSuccess });
        }


        //[HttpPost]
        //public ActionResult Delete(string id)
        //{
        //    var hasPermission = religionService.DeletePermission(LoginInfo.AccessCode);
        //    if (hasPermission)
        //    {
        //        bool success = false;
        //        foreach (var item in id.Split(",", StringSplitOptions.RemoveEmptyEntries))
        //        {
        //            success = religionService.DeleteReligion(item);
        //        }

        //        return Json(new { success = success, message = "Deleted Successfully" });
        //    }
        //    else
        //    {
        //        return Json(new { isSuccess = false, message = "You have no access" });
        //    }

        //}


        [HttpPost]
        public JsonResult CheckAvailability(string name, string code)
        {
            if (religionService.IsReligionExist(name, code))
            {
                return Json(new { isSuccess = true, message = "Already Exists" });
            }

            return Json(new { isSuccess = false });
        }
    }
}