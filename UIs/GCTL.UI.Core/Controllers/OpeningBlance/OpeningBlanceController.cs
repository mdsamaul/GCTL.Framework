using GCTL.Core.ViewModels.OpeningBalance;
using GCTL.Service.OpeningBalance;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.OpeningBlance
{
    public class OpeningBlanceController : BaseController
    {
        private readonly IOpeningBalance _service;

        public OpeningBlanceController(IOpeningBalance service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [Route("opening-balance/details/{id}")]
        public async Task<IActionResult> GetOpeningBalanceeDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Opening Balance with ID {id} not found." });

            return Ok(data);
        }


        [HttpPost]
        [Route("OpeningBlance/GetGridData")]
        public async Task<IActionResult> GetGridData(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "ComOpeningBalanceCode",
            string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        draw,
                        data = new List<OpeningBalanceVM>(),
                        recordsTotal = 0,
                        recordsFiltered = 0
                    });
                }

                return Ok(new
                {
                    draw,
                    data = result.Data,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.TotalCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("opening-balance-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "ComOpeningBalanceCode", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<OpeningBalanceVM>(),
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
                        Message = "No Opening Balance Found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }



        [HttpPost]
        [Route("opening-balance")]
        public async Task<ActionResult> Create([FromBody] OpeningBalanceVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    //return BadRequest(ModelState);
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                               .Select(e => e.ErrorMessage)
                               .ToList();
                    return BadRequest(new { errors });
                }

                if (model == null)
                    return BadRequest(new { message = "Opening balance  is Required." });

                var result = await _service.SaveAsync(model);


                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });



                return Ok(new { success = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("opening-balance/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] OpeningBalanceVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Opening Balance with ID {id} Not Found." });
            
            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }


        [HttpDelete]
        [Route("opening-balance-all-delete")]
        public async Task<ActionResult> BulkDelete([FromBody] List<decimal> ids)
        {
            try
            {
                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Data Is Selected To Delete" });
                }

                var result = await _service.BulkDeleteAsync(ids);
                if (!result)
                {
                    return Json(new { isSuccess = false, message = "No Data Found to Delete" });
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


        #region Company & Branch & General Ledger Dropdown


        [HttpGet]
        [Route("openingUI-General-Ledger-dropdown")]
        public async Task<IActionResult> GeneralLedgerDropdown()
        {
            var list = await _service.GetGenetalLedegerDropdownInfo();
            return Ok(new { data = list });
        }


        [HttpGet]
        [Route("openingUI-company-dropdown")]
        public async Task<IActionResult> CompanyDropdown()
        {
            var list = await _service.GetCompanyDropdownInfo();
            return Ok(new { data = list });
        }

        [HttpGet]
        [Route("openingUI-branch-dropdown")]
        public async Task<IActionResult> BranchDropdown(string companycode)
        {
            var list = await _service.GetBranchDropdownInfo( companycode);
            return Ok(new { data = list });
        }

        #endregion
    }
}
