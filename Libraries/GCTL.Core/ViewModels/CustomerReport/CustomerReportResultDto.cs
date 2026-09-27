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
        public string ContactPerson { get; set; }
        public byte[] CustomerPhoto { get; set; }
        public byte[] CustomerDigitalSignature { get; set; }
        public string? PhotoUrl { get; set; }
        public string? SignatureUrl { get; set; }
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
