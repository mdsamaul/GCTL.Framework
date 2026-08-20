using GCTL.Service.Services;
using Microsoft.AspNetCore.Http;

namespace GCTL.Core.Interfaces.Services
{
    public interface IHrmEmployeeBulkImportService
    {
        // BulkImportResult class টা HrmEmployeeBulkImportService.cs এ define করা আছে
        Task<BulkImportResult> BulkImportFromExcelAsync(
            IFormFile file,
            string companyCode,
            string luser,
            string lip,
            string lmac);

        byte[] GenerateExcelTemplate();
    }
}