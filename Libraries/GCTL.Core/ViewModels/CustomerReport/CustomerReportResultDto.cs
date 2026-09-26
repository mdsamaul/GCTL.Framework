using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.CustomerReport
{
    public class CustomerReportResultDto
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCompany { get; set; }
        public string CustomerAddress { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public string Fax { get; set; }
        public string Url { get; set; }
        public string Email1 { get; set; }
        public string Category { get; set; }
        public string SalesPersonId { get; set; }
        public string Bin { get; set; }
        public string EmployeeId { get; set; }
        public string CustomerCode { get; set; }
        public string CountryId { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string ZipCode { get; set; }
        public string VatRegNo { get; set; }
        public string Tin { get; set; }
        public string ContactPerson { get; set; }
        public string DevitOrCredit { get; set; }
        public DateTime? OpeningDate { get; set; }
        public string ShortName { get; set; }
        public byte[] CustomerPhoto { get; set; }
        public byte[] CustomerDigitalSignature { get; set; }
        public string CustomerTypeId { get; set; }
        public string CustomerType { get; set; }
        public string Cpid { get; set; }
        public string ContactPersonName { get; set; }
        public string DesignationCode { get; set; }
        public string Designation { get; set; }
        public string ContactPersonMobile { get; set; }
        public string ContactPersonEmail { get; set; }
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress1 { get; set; }
    }
}
