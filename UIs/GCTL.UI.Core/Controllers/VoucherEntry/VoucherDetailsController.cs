using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.VoucherEntry;
using GCTL.Service.OpeningBalance;
using GCTL.Service.VoucherEntry;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL_App.Controllers.VoucherEntry
{
    public class VoucherDetailsController : BaseController
    {

        #region Service

        private readonly IVoucherDetails _service;
        private readonly IOpeningBalance _openingBalance;
        public VoucherDetailsController(IVoucherDetails service, IOpeningBalance openingBalance) : base()
        {
            _service = service;
            _openingBalance = openingBalance;
        }

        #endregion


        #region Account Head Dropdown on Event Change Backend
        [HttpGet]
        [Route("Ledger-details/{id}")]
        public async Task<IActionResult> GetLedgerDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Invalid ID." });


            var data = await _service.GetAllLedgerByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Ledger with ID {id} not found." });

            return Ok(data);
        }
        #endregion


        #region Get Details Voucher Information with specific ID

        [HttpGet]
        [Route("tmp-voucher/details/{id}")]
        public async Task<IActionResult> GetVoucherTmpDetails(decimal id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Voucher Tmp Details with ID {id} not found." });

            return Ok(data);
        }
        #endregion


        #region Get All Tmp Voucher Details
        [HttpGet]
        [Route("all-tmp-voucher-details")]
        public async Task<IActionResult> GetAllTmpVoucherDetails()
        {
            try
            {
                string currentUserId = LoginInfo?.Username;

                int.TryParse(currentUserId, out int employeeId);

                //var data = await _service.GetAllAsync(employeeId);
                var data = await _service.GetAllAsync(currentUserId);

                if (data == null) data = new List<VoucherEntryDetailsTempVM>();
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error while fetching data.", error = ex.Message });
            }
        }

        #endregion


        #region Tmp Details Voucher Entry Save and Update

        [HttpPost]
        [Route("tmp-voucher-details")]
        public async Task<ActionResult> Create([FromBody] VoucherEntryDetailsTempVM model)
        {
            try
            {  
                if (model == null) return BadRequest(new { message = "Validation Failed." });

                model.Luser = LoginInfo?.EmployeeId;
                model.ToAudit(LoginInfo);
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
        [Route("tmp-voucher-details/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] VoucherEntryDetailsTempVM model)
        {

            if (model == null || id != model.autoId)  return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Voucher Tmp with ID {id} Not Found." });

            model.Luser = LoginInfo?.EmployeeId;
            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }

        #endregion


        #region Delete Voucher Tmp Details

        [HttpDelete]
        [Route("tmp-voucher-all-delete")]
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

        #endregion


        #region Get All
        //public async Task<IActionResult> GetAll()
        //{
        //    var data = await _service.GetAllAsync();
        //    return Ok(new { data = data });
        //}
        #endregion


        #region Account Head Dropdown

        [HttpGet]
        [Route("Account-Head-dropdown")]
        public async Task<IActionResult> AccountHeadDropdown()
        {
            var list = await _service.GetAccountHeadDropdownAsync();
            return Ok(new { data = list });
        }

        #endregion

    }
}
