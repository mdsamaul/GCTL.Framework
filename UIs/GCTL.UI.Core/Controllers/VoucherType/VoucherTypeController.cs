using GCTL.Core.ViewModels.VoucherType;
using GCTL.Service.VoucherType;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL_App.Controllers.VoucherType
{
    public class VoucherTypeController : BaseController
    {
        private readonly IVoucherType _service;
        public VoucherTypeController( IVoucherType service) : base()
        {
            _service = service;
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [Route("voucher-type/details/{id}")]
        public async Task<IActionResult> GetVoucherTypeDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Voucher Type with ID {id} not found." });

            return Ok(data);
        }


        [HttpGet]
        [Route("voucher-type-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherType_Code", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<VoucherTypeVM>(),
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
                        Message = "No Voucher Type Found."
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
        [Route("voucher-type/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] VoucherTypeVM model)
        {
            if (model == null) return BadRequest(new { message = "Invalid Data." });

            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
            {
                return Ok(new { isDuplicate = true, message = "Data Already Exists." });
            }

            return Ok(new { isDuplicate = false, message = "No Duplicate Found." });
        }

        [HttpPost]
        [Route("voucher-type")]
        public async Task<ActionResult> Create([FromBody] VoucherTypeVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "Voucher Type Name is Required." });

                var result = await _service.SaveAsync(model);


                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });



                return Ok(new {success = true, message = "Data Saved Successfully."});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("voucher-type/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] VoucherTypeVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Voucher Type with ID {id} Not Found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }


        [HttpDelete]
        [Route("voucher-type-all-delete")]
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

        #region Reset Duration Type Dropdown

        [HttpGet]
        [Route("reset-duration-dropdown")]
        public async Task<IActionResult> GetDurationType()
        {
            var list = await _service.DurationTypeDropdown();
            return Ok(new { data = list });
        }

        #endregion
    }
}
