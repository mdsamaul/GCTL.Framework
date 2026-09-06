using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClearAndF.Update
{
    public class JobDto
    {
        public int Id { get; set; }
        public string JobNo { get; set; }
        public string ShipmentMode { get; set; }
        public string JobDate { get; set; }         // formatted as "yyyy-MM-dd"
        public string CustomerName { get; set; }
        public string? DocReceivedDate { get; set; } // can be null, formatted "yyyy-MM-dd"
    }

    public class SepPagedResult<T>
    {
        public List<T> Data { get; set; } = new();

        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }
}
