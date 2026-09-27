using GCTL.Core.ViewModels.DocumentationReport;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GCTL.Service.DocumentationReport
{
    public interface IDocumentationReportService
    {
        Task<List<DocumentationReportItemDTO>> GetDocumentationReportDataAsync(DocumentationReportFilterVM filter);
        Task<byte[]> GeneratePdfReportAsync(DocumentationReportFilterVM filter);
        Task<byte[]> GenerateExcelReportAsync(DocumentationReportFilterVM filter);
    }
}
