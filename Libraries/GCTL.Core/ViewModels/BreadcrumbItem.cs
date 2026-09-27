using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels
{
    public class BreadcrumbItem
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public bool IsActive { get; set; }
    }
}
