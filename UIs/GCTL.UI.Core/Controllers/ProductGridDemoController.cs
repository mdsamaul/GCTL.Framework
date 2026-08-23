using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels;

namespace GCTL.UI.Core.Controllers
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    /// <summary>
    /// Controller demonstrating DynamicGrid usage with HasCheckbox = false and numeric PrimaryKey.
    /// </summary>
    public class ProductGridDemoController : Controller
    {
        private static readonly List<ProductDto> MockProducts = Enumerable.Range(1, 32).Select(i => new ProductDto
        {
            ProductId = i,
            Code = $"PRD-{5000 + i}",
            Name = $"Product Item {i}",
            Category = i % 2 == 0 ? "Electronics" : "Hardware",
            Price = 49.99m * i,
            StockQuantity = i * 15
        }).ToList();

        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            var gridModel = PrepareProductGrid(page, pageSize);
            return View(gridModel);
        }

        [HttpGet]
        public IActionResult GetProductGridPartial(int page = 1, int pageSize = 10)
        {
            var gridModel = PrepareProductGrid(page, pageSize);
            return PartialView("_DynamicGrid", gridModel);
        }

        private DynamicGridModel PrepareProductGrid(int page, int pageSize)
        {
            int totalRecords = MockProducts.Count;
            var pagedData = MockProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var headers = new List<string> { "Product Code", "Product Name", "Category", "Price", "Stock Quantity" };

            return GridBuilder.BuildGrid(
                data: pagedData,
                totalRecords: totalRecords,
                currentPage: page,
                pageSize: pageSize,
                headers: headers,
                rowMapper: product => new Dictionary<string, object?>
                {
                    { "Product Code", product.Code },
                    { "Product Name", product.Name },
                    { "Category", product.Category },
                    { "Price", $"${product.Price:N2}" },
                    { "Stock Quantity", product.StockQuantity }
                },
                ajaxUrl: Url.Action("GetProductGridPartial", "ProductGridDemo") ?? "/ProductGridDemo/GetProductGridPartial",
                primaryKey: "Product Code",
                hasCheckbox: false, // Checkboxes disabled for Product grid
                actionUrlPattern: null,
                gridId: "productGridContainer"
            );
        }
    }
}
