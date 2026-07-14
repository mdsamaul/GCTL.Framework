using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.AddSalesCustomer
{
    public class SalesCustomerViewModel
    {
        public string CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string ShortName { get; set; }
        public string CustomerAddress { get; set; }
        public string? CountryId { get; set; }
        public string? City { get; set; }
        public string? StateOrProvince { get; set; }
        public string? ZipCode { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Url { get; set; }
        public string? Bin { get; set; }
        public string? VatRegNo { get; set; }
        public string? Tin { get; set; }
        public string? ContactPerson { get; set; }
        public decimal? OpeningBalance { get; set; } = 0.0m;
        public DateTime? OpeningDate { get; set; }
        public decimal? CreditLimit { get; set; } = 0.0m;
        public string? CustomerType { get; set; }
        public string? Category { get; set; }
        public string? SalesPerson { get; set; }
        public string? TransactionType { get; set; }

        public string CompanyCode { get; set; } = null!;

        public string EmployeeId { get; set; } = null!;


        public DateTime? LDate { get; set; } = DateTime.Now;
        public DateTime? ModifyDate { get; set; }

        public List<SalesDeliveryLocationViewModel> DeliveryLocations { get; set; } = new List<SalesDeliveryLocationViewModel>();
        public List<SalesContactPersonViewModel> ContactPersons { get; set; } = new List<SalesContactPersonViewModel>();


        //Others Property for Saving Empty String
        //public string? ContatPerson1 { get; set; }

        //public string? Designation1 { get; set; }

        //public string? phone1 { get; set; }

        //public string? ContatPerson2 { get; set; }

        //public string? Designation2 { get; set; }

        //public string? phone2 { get; set; }

        //public string? ContatPerson3 { get; set; }

        //public string? Designation3 { get; set; }

        //public string? phone3 { get; set; }
        //public string? Email1 { get; set; }

        //public string? Email2 { get; set; }

        //public string? Email3 { get; set; }
        //public string? LIP { get; set; }

        //public string? LMAC { get; set; }
        //public string? DevitOrCredit { get; set; }

    }
}
