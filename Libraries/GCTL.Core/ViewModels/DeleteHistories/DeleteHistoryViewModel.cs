using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.DeleteHistories
{
    public class DependencyCheckResult
    {
        public bool CanDelete { get; set; }
        public List<string> DependentTables { get; set; }
        public string Message { get; set; }

        public DependencyCheckResult()
        {
            DependentTables = new List<string>();
        }
    }

    public class DeleteHistoryViewModel : BaseViewModel
    {
        public string? tableName { get; set; }
        public string? CompanyCode { get; set; }
    }
}
