using GCTL.Core.Data;
using GCTL.Data.Models;
using GCTL.Service.BankBranchInformations;
using GCTL.Service.BankInformations;
using GCTL.Service.BloodGroups;
using GCTL.Service.BranchesTypeInfo;
using GCTL.Service.Companies;
using GCTL.Service.Country;
using GCTL.Service.Departments;
using GCTL.Service.Designations;
using GCTL.Service.Nationalitys;
using GCTL.Service.PaymentModes;
using GCTL.Service.Religions;
using GCTL.Service.SupplierCategory;
using GCTL.Service.SupplierOrigin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace GCTL.UI.Core.Controllers
{
    public class CascadingController : Controller
    {
        private readonly IBloodGroupService bloodGroupService;
        private readonly IReligionService religionService;
        private readonly IDesignationService designationService;
        private readonly IDepartmentService departmentService;
        private readonly IPaymentModeService paymentModeService;
        private readonly ICompanyService companyService;
        private readonly IBankInformationsService bankInformationsService;
        private readonly ISalesDefBankBranchInfosService salesDefBankBranchInfosService;
        private readonly IBranchTypeInfoService branchTypeInfoService;
        public readonly INationalitysService nationalitysService;
        private readonly ISupplierCategoryService supplierCategoryService;
        private readonly ICountryService countryService;
        public CascadingController(
            IBloodGroupService bloodGroupService,
            IReligionService religionService,
            IDesignationService designationService,
            IDepartmentService departmentService,
            IPaymentModeService paymentModeService,
            ICompanyService companyService,
            IBankInformationsService bankInformationsService,
            ISalesDefBankBranchInfosService salesDefBankBranchInfosService,
             IBranchTypeInfoService branchTypeInfoService,
             INationalitysService nationalitysService,
             ISupplierOriginService supplierOriginService,
             ISupplierCategoryService supplierCategoryService,
             ICountryService countryService

            )
        {
            this.bloodGroupService = bloodGroupService;
            this.religionService = religionService;
            this.designationService = designationService;
            this.departmentService = departmentService;
            this.paymentModeService = paymentModeService;
            this.bankInformationsService = bankInformationsService;
            this.salesDefBankBranchInfosService = salesDefBankBranchInfosService;
            this.branchTypeInfoService = branchTypeInfoService;
            this.nationalitysService = nationalitysService;
            this.supplierCategoryService = supplierCategoryService;
            this.countryService = countryService;
            this.companyService = companyService;
        }


        public IActionResult GetBankInfo()
        {
            return Json( bankInformationsService.BankDropSelectionAsync());
        }
        

        public async Task<IActionResult> GetCompany()
        {
            return Json(await companyService.GetCompanyDropDown());
        }

        public IActionResult GetBank()
        {
            return Json(bankInformationsService.BankDropSelectionAsync());
        }

        public IActionResult GetBankBranch()
        {
            return Json(salesDefBankBranchInfosService.BankBranchDropSelectionAsync());
        }
        
        public IActionResult GetBloodGroups()
        {
            return Json(bloodGroupService.BloodGroupSelection());
        }

        public IActionResult GetReligions()
        {
            return Json(religionService.ReligionSelection());
        }

        public IActionResult GetDesignations()
        {
            return Json(designationService.DesignationSelection());
        }

        public IActionResult GetDepartments()
        {
            return Json(departmentService.DepartmentSelection());
        }

        public IActionResult GetPaymentModes()
        {
            return Json(paymentModeService.PaymentModeSelection());
        }

        
        [HttpPost]
       
       

        public async Task< IActionResult> GetCompanieBranchSelections()
        {
            var data = await branchTypeInfoService.GetCompanieBranchSelections();
            return Json(data);
        }

        public async Task<IActionResult> GetNationalitys()
        {
            return Json(await nationalitysService.SelectionNationalityAsync());
        }

        public async Task<IActionResult> GetSupplierCata()
        {
            return Json(await supplierCategoryService.SelectionSupplierCategoryAsync());
        }

        public async Task<IActionResult> GetCountrys()
        {
            return Json(await countryService.SelectionCountryAsync());
        }

        
    }
}
