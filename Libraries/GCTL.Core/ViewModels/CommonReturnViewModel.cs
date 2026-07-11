using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels
{
    public class CommonReturnViewModel
    {
        public bool Success { get; set; }
        public bool RefError { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public List<string> Errors { get; set; } 

        public CommonReturnViewModel()
        {
            Errors = new List<string>(); 
        }
    }
}
