using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Controller demonstrating DynamicGrid usage with checkboxes, custom primary key, and dynamic link generation.
    /// </summary>
    public class CustomerGridDemoController : Controller
    {
        // Mock dataset of 45 customers
        private static readonly List<CustomerDto> MockCustomers = Enumerable.Range(1, 45).Select(i => new CustomerDto
        {
            CustomerId = $"CUST-{1000 + i}",
            Name = $"Customer {i}",
            Email = $"customer{i}@example.com",
            Company = i % 2 == 0 ? "Global Logistics Ltd" : "Apex Solutions",
            Status = i % 3 == 0 ? "Inactive" : "Active",
            CreatedDate = DateTime.Now.AddDays(-i)
        }).ToList();

        /// <summary>
        /// Renders main parent view with initial grid partial.
        /// </summary>
        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            var gridModel = PrepareCustomerGrid(page, pageSize);
            return View(gridModel);
        }

        /// <summary>
        /// AJAX endpoint for page switching requests. Returns PartialView "_DynamicGrid".
        /// </summary>
        [HttpGet]
        public IActionResult GetCustomerGridPartial(int page = 1, int pageSize = 10)
        {
            var gridModel = PrepareCustomerGrid(page, pageSize);
            return PartialView("_DynamicGrid", gridModel);
        }

        /// <summary>
        /// Mock details endpoint for dynamic primary key links.
        /// </summary>
        public IActionResult Details(string id)
        {
            var customer = MockCustomers.FirstOrDefault(c => c.CustomerId == id);
            if (customer == null) return NotFound();
            return Content($"Customer Details Page for {customer.Name} ({customer.CustomerId})");
        }

        private DynamicGridModel PrepareCustomerGrid(int page, int pageSize)
        {
            int totalRecords = MockCustomers.Count;
            var pagedData = MockCustomers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var headers = new List<string> { "Customer ID", "Customer Name", "Email Address", "Company", "Account Status", "Registered Date" };

            return GridBuilder.BuildGrid(
                data: pagedData,
                totalRecords: totalRecords,
                currentPage: page,
                pageSize: pageSize,
                headers: headers,
                rowMapper: customer => new Dictionary<string, object?>
                {
                    { "CustomerId", customer.CustomerId },
                    { "Customer Name", customer.Name },
                    { "Email Address", customer.Email },
                    { "Company", customer.Company },
                    { "Account Status", customer.Status },
                    { "Registered Date", customer.CreatedDate.ToString("yyyy-MM-dd") }
                },
                ajaxUrl: Url.Action("GetCustomerGridPartial", "CustomerGridDemo") ?? "/CustomerGridDemo/GetCustomerGridPartial",
                primaryKey: "CustomerId",
                hasCheckbox: true,
                actionUrlPattern: "/CustomerGridDemo/Details/{id}",
                linkColumn: "Customer Name",
                gridId: "customerGridContainer"
            );
        }
    }
}
