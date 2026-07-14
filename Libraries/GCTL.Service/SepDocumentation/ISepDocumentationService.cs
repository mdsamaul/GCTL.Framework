using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.SepDocumentation;
using System.Collections;

namespace GCTL.Service.SepDocumentation
{
    public interface ISepDocumentationService
    {
        Task<CommonReturnViewModel> AddDocumentation(GCTL.Data.Models.SepDocumentation model);
        Task<CommonReturnViewModel> UpdateDocumentation(GCTL.Data.Models.SepDocumentation model);
        Task<CommonReturnViewModel> DeleteDocumentation(string jobNo);
        Task<(List<DocumentationResult> Data, int TotalRecords)> GetDocumentationList(int page, int pageSize, string search, string sortColumn, string sortDirection, DateTime? dateFrom, DateTime? dateTo, string customerId);
        List<SelectListItem> GetAddresses();
        List<SelectListItem> GetCustomers();
        GCTL.Data.Models.SepDocumentation GetDocumentationByTC(decimal tc);
    }
}