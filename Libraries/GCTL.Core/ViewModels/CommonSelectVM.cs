using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels
{
    public class CommonSelectVM
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? GroupName { get; set; }
    }

    public class CommonChoiceVM
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public bool HasMore { get; set; }
    }
}
