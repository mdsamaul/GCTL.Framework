using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Service.ChartOfAccounts.ControlLedger;
using GCTL.Service.ChartOfAccounts.GeneralLedger;
using GCTL.Service.ChartOfAccounts.SubControlLedger;
using GCTL.Service.ChartOfAccounts.SubSidiaryLedger;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class GeneralLedgerController : BaseController
    {
        private readonly ISubSidiaryLedger _subsidiaryLedger;
        private readonly ISubControlLedger _subcontrolLedger;
        private readonly IContrlLedger _controlLedger;
        private readonly IGeneralLedger _service;
        public GeneralLedgerController(ISubSidiaryLedger subsidiaryLedger, ISubControlLedger subControlLedger, IContrlLedger controlLedger, IGeneralLedger service) : base()
        {
            _subsidiaryLedger = subsidiaryLedger;
            _subcontrolLedger = subControlLedger;
            _controlLedger = controlLedger;
            _service = service;
        }

        [HttpGet]
        [Route("genral-ledger/details/{id}")]
        [Route("general-ledger/details/{id}")]
        public async Task<IActionResult> GetGeneralLedgerDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"General Ledger with ID {id} not found." });

            return Ok(data);
        }


        [HttpGet("next-general-ledger-code")]
        public async Task<IActionResult> GenerateNewIdAsync(string subsidiaryLedgercode)
        {
            var newCode = await _service.GetLastGeneralLedgerCodeAsync(subsidiaryLedgercode);
            return Ok(newCode);
        }


        [HttpGet]
        [Route("general-ledger-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubSusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, groupLedgerCode, controlLedgerCode, subcontrolLedgerCode, subSididaryLedger);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<GeneralLedgerVM>(),
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
                        Message = "No General Ledger Found."
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
        [Route("general-ledger/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] GeneralLedgerVM model)
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
        [Route("general-ledger")]
        public async Task<ActionResult> Create([FromBody] GeneralLedgerVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "General Ledger is Required." });

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
        [Route("general-ledger/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] GeneralLedgerVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"General Ledger with ID {id} Not Found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }


        [HttpDelete]
        [HttpPost]
        [Route("general-ledger-all-delete")]
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

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> GetAll(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "", string subsidiaryLedgerCode = "")
        {
            var subLedger = !string.IsNullOrEmpty(subSididaryLedger) ? subSididaryLedger : subsidiaryLedgerCode;
            var data = await _service.GetAllAsync(groupLedgerCode, controlLedgerCode, subcontrolLedgerCode, subLedger);
            return Ok(new { data = data });
        }


        #region Dropdown Section

        #region Group Ledger 

        //Group Ledger Dropdown

        [HttpGet]
        [Route("generalUi-grl-dropdown")]
        public async Task<IActionResult> GetAllGroupLedger()
        {
            var list = await _controlLedger.GroupLedgerDropdown();
            return Ok(new { data = list });
        }

        //Group Ledger related Info

        [HttpGet]
        [Route("generalUi-grl-info")]
        public async Task<IActionResult> GetGroupLedger(string groupcode)
        {
            var ledger = await _controlLedger.GetAllGroupLedger(groupcode);

            if (ledger == null) return NotFound();

            return Ok(ledger);
        }

        #endregion


        #region Control Ledger

        //Control Ledger Dropdown
        [HttpGet]
        [Route("generalUi-crl-dropdown")]
        public async Task<IActionResult> GetControlLedgerDropdown(string groupcode)
        {
            var list = await _subcontrolLedger.ControlLedgerDropdown(groupcode);
            return Ok(new { data = list });
        }

        //Control Ledger Info

        [HttpGet]
        [Route("generalUi-crl-info")]
        public async Task<IActionResult> GetControlLedgerInfo(string contrlcode)
        {
            var ledger = await _subcontrolLedger.GetAllControlInfo(contrlcode);

            if (ledger == null)  return NotFound();

            return Ok(ledger);
        }

        #endregion


        #region Sub-Control Ledger

        //Sub Control Ledger
        [HttpGet]
        [Route("generalUi-subcrl-dropdown")]
        public async Task<IActionResult> GetSubControlLedgerDropdown(string controlcode)
        {
            var list = await _subsidiaryLedger.SubControlLedgerDropdown(controlcode);
            return Ok(new { data = list });
        }

        //Sub Control Info
        [HttpGet]
        [Route("generalUi-subcrl-info")]
        public async Task<IActionResult> GetSubControlLedgerInfo(string subcontrlcode)
        {
            var ledger = await _subsidiaryLedger.SubControlLedgerInfo(subcontrlcode);

            if (ledger == null) return NotFound();

            return Ok(ledger);
        }

        #endregion


        #region Sub-Sidiary Ledger

        //Sub Sidiary Ledger
        [HttpGet]
        [Route("generalUi-ssl-dropdown")]
        public async Task<IActionResult> GetSubSidiaryLedgerDropdown(string subcontrolcode)
        {
            var list = await _service.SubSidiaryLedgerDropdown(subcontrolcode);
            return Ok(new { data = list });
        }

        //Sub Sidiary Info
        [HttpGet]
        [Route("generalUi-ssl-info")]
        public async Task<IActionResult> GetSubSidiaryLedgerInfo(string subsidiarycode)
        {
            var ledger = await _service.SubSidiaryLedgerInfo(subsidiarycode);

            if (ledger == null) return NotFound(); 

            return Ok(ledger);
        }

        #endregion


        #region Cash Flow

        //Cash Flow Dropdown

        [HttpGet]
        [Route("cash-flow-dropdown")]
        public async Task<IActionResult> GetAllCashFlow()
        {
            var list = await _service.CashFlowDropdown();
            return Ok(new { data = list });
        }

        #endregion

        #endregion
    }
}
