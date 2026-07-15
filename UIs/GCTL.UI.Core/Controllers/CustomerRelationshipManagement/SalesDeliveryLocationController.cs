using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Service.CustomerRelationshipManagement.AddContactPerson;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.CustomerRelationshipManagement.AddSalesDeliveryLocation;
using GCTL.UI.Core.Controllers;
using GCTL_App.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CustomerRelationshipManagement
{
    public class SalesDeliveryLocationController : BaseController
    {
        //private readonly ISalesCustomerService _salesCustomerService;
        private readonly ISalesDeliveryLocationService _salesDeliveryLocation;
        private readonly ISalesCustomerService _saleCusterService;
        private readonly IContactPersonService _contactPersonService;
        public SalesDeliveryLocationController(ISalesDeliveryLocationService salesDeliveryLocationService, ISalesCustomerService saleCusterService, IContactPersonService contactPersonService) : base()
        {
            _salesDeliveryLocation = salesDeliveryLocationService;
            _saleCusterService = saleCusterService;
            _contactPersonService = contactPersonService;
        }

        public async Task<IActionResult> GetAllByCustomerId(string cusId)
        {
            var deliveryAddress = await _salesDeliveryLocation.GetDeliveryLocationByCustomer(cusId);

            return Ok(deliveryAddress);
        }

        public async Task<IActionResult> GetAllPaginated(string cusId ="", int pageNumber = 1, int pageSizeDl = 10, string searchTerm = "", string sortColumn = "DeliveryLocationCode", string sortOrder = "desc")
        {
            try
            {
                var result = await _salesDeliveryLocation.GetPaginatedDeliveryLocations(cusId, pageNumber, pageSizeDl, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || result.Data.Count() == 0)
                {
                    return NotFound(new { message = "No customers found." });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        public async Task<string> GenerateNewSalesDeliveryLocationCode()
        {
            var lastDLCode = await _salesDeliveryLocation.GenerateDeliveryLocationIdAsync();

            string newCode;

            if (!string.IsNullOrEmpty(lastDLCode))
            {
              
                if (int.TryParse(lastDLCode, out int numericPart))
                {
                    newCode = (numericPart + 1).ToString("D8");
                }
                else
                {
                    throw new InvalidOperationException("Invalid DeliveryLocationCode format.");
                }
            }
            else
            {
                
                newCode = "00000001";
            }

            return newCode;
        }

        public async Task<IActionResult> Create(SalesDeliveryLocationViewModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Delivery Location's data is required." });

                var result = await _salesDeliveryLocation.SaveAsyncDeliveryLocation(model);


                if (!result)
                    return BadRequest(new { message = "Failed to create customer." });



                return Json(new { isSuccess = true, message = "Saved Successfully.", });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            try
            {
                var customer = await _salesDeliveryLocation.GetByIdAsync(id);

                if (customer == null)
                    return NotFound(new { message = $"Delivery Location with ID {id} not found." });

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Edit(string id, [FromForm] SalesDeliveryLocationViewModel model)
        {
            try
            {
                Console.WriteLine($"================Edit hit with id: {id}, DeliveryLocationCode from model: {model?.DeliveryLocationCode}");

                if (model == null || id != model.DeliveryLocationCode)
                    return BadRequest(new { message = "Delivery Location data is invalid." });

                var customerExists = await _salesDeliveryLocation.GetByIdAsync(id);
                if (customerExists == null)
                    return NotFound(new { message = $"Delivery Location with ID {id} not found." });

                var result = await _salesDeliveryLocation.UpdateAsyncDeliveryLocation(model);

                if (!result)
                    return BadRequest(new { message = "Failed to update Delivery Location." });

                return Ok(new { message = "Delivery Location updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string? id)
        {
            try
            {
                var result = await _salesDeliveryLocation.DeleteAsyncDeliveryLocation(id);

                if (!result)
                    return NotFound(new { message = $"Delivery Location with ID {id} not found." });

                return Ok(new { success = true, message = "Data Deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        public async Task<ActionResult> BulkDelete(List<string> ids)
        {
            try
            {
                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Data is selected to delete." });
                }

                var result = await _salesDeliveryLocation.BulkDeleteAsync(ids);
                if (!result)
                {
                    return Json(new { isSuccess = false, message = $"No delivery Location is found to delete" });
                }
                return Json(new { isSuccess = true, message = $" Data Deleted  Successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        //Country Dropdown for Delivery Location
        [HttpGet]
        public async Task<IActionResult> GetCountryDropdown()
        {
            var list = await _saleCusterService.GetCountryDropdownAsync();
            return Ok(list);
        }

        //Contact Person Dropdown for Delivery Location
        [HttpGet]
        public async Task<IActionResult> GetContactPerson()
        {
            var list = await _contactPersonService.DropdownContact();
            return Ok(list);
        }

    }
}
