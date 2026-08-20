using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.SepDocumentation
{
    public class CustomerInfoDto
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? ShortName { get; set; }
        public string? CustomerAddress { get; set; }
        public List<DelivaryAddressDto>? DelivaryAddress { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
