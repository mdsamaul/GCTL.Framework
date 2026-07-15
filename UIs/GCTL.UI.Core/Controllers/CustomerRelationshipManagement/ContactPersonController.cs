using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Service.Companies;
using GCTL.Service.CustomerRelationshipManagement.AddContactPerson;
using GCTL.Service.Designations;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CustomerRelationshipManagement
{
    public class ContactPersonController : BaseController
    {
        private readonly IContactPersonService _service;
        private readonly IDesignationService _hrmDefDesignationService;
        private readonly ICompanyService _coreCompany;

        public ContactPersonController(IContactPersonService service, IDesignationService hrmDefDesignationService, ICompanyService coreCompany) : base()
        {
            _service = service;
            _hrmDefDesignationService = hrmDefDesignationService;
            _coreCompany = coreCompany;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ContactPersonForm() => PartialView("ContactPersonForm");

        [HttpGet]
        public IActionResult ContactPersonGrid() => PartialView("ContactPersonGrid");

        [HttpGet("next-CPID-id")]
        public async Task<string> GenerateNewIdAsync()
        {
            var lastId = await _service.GetLastCPIDAsync();

            string newId;

            if (!string.IsNullOrEmpty(lastId) && lastId.StartsWith("CP"))
            {
                string numericPart = lastId.Substring(2);

                if (int.TryParse(numericPart, out int lastNumericId))
                {
                    int nextId = lastNumericId + 1;
                    newId = "CP" + nextId.ToString("D5");
                }
                else
                {
                    newId = "CP00001";
                }
            }
            else
            {
                newId = "CP00001";
            }

            return newId;
        }


        [HttpGet]
        [Route("contact-person-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "CPID", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<SalesContactPersonViewModel>(),
                        TotalCount = 0,
                        PaginationInfo = new
                        {
                            StartItem = 0,
                            EndItem = 0,
                            TotalItems = 0,
                            PageNumbers = new List<int>(),
                            TotalPages = 0,
                            CurrentPage = 0
                        },
                        Message = "No Bank Found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("contact/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] SalesContactPersonViewModel model)
        {
            if (model == null) return BadRequest(new { message = "Invalid data." });

            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
            {
                return Ok(new { isDuplicate = true, message = "Data Already Exists." });
            }

            return Ok(new { isDuplicate = false, message = "No duplicate found." });
        }

        [HttpPost]
        [Route("contact-person")]
        public async Task<ActionResult> Create([FromBody] SalesContactPersonViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "Contact person is required." });

                var result = await _service.SaveAsync(model);


                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });



                return Ok(new { success = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("contact-person/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] SalesContactPersonViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.AutoId)
                return BadRequest(new { message = "Data is invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Contact Person with ID {id} not found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data updated successfully." });
        }

        [HttpDelete]
        [Route("single-contact-person/{id}")]
        public async Task<IActionResult> Delete(decimal id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);

                if (!result)
                    return NotFound(new { message = $"Contact Person with ID {id} not found." });

                return Ok(new { isSuccess = true, message = "Data Deleted Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {  message = "An error occurred.", error = ex.Message });
            }
        }
        [HttpDelete]
        [Route("contact-person-list")]
        public async Task<ActionResult> BulkDelete([FromBody] List<decimal> ids)
        {
            try
            {
                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Data is selected to delete" });
                }

                var result = await _service.BulkDeleteAsync(ids);
                if (!result)
                {
                    return Json(new { isSuccess = false, message = "No Data found to delete" });
                }
                return Json(new { isSuccess = true, message = $"Data Deleted Successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data = data });
        }

        [HttpGet("designation-dropdown")]
        //public async Task<IActionResult> GetAllDesignation()
        //{
        //    var list = await _hrmDefDesignationService.DesignationDropdown();
        //    return Ok(list);
        //}

        [HttpGet("core-company-dropdown")]
        //public async Task<IActionResult> GetAllCompany()
        //{
        //    var list = await _coreCompany.DropdownCompany();
        //    return Ok(list);
        //}

        [HttpGet("contact-person-details")]
        public async Task<ActionResult> Details(decimal id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);

                if (data == null)
                    return NotFound(new { message = $"Contact Person with ID {id} not found." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }
    }
}
