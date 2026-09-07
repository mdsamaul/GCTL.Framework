using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.DataTables;

namespace GCTL.Core.ViewModels.ClearAndF.Update
{
    public class JobDto
    {
        public int Id { get; set; }
        [Searchable]
        public string JobNo { get; set; } = string.Empty;
        [Searchable]
        public DateTime? JobDate { get; set; }
        [Searchable]
        public string ShipmentMode { get; set; } = string.Empty;
        [Searchable]
        public string CustomerName { get; set; } = string.Empty;
        [Searchable]
        public string CustomerAddress { get; set; } = string.Empty;
        [Searchable]
        public DateTime? DocReceivedDate { get; set; }
        [Searchable]
        public string Invoice { get; set; } = string.Empty;
        [Searchable]
        public string Beno { get; set; } = string.Empty;
    }

    public class SepPagedResult<T>
    {
        public List<T> Data { get; set; } = new();

        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
    }
}
