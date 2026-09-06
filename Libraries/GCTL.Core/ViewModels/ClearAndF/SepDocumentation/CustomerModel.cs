using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClearAndF.SepDocumentation
{
    public class CustomerModel : BaseViewModel
    {
        public string CustomerID { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCompany { get; set; }
        public string CustomerAddress11 { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ShortName { get; set; }
        public string ContatPerson1 { get; set; }
        public string Designation1 { get; set; }
        public string phone1 { get; set; }
        public string ContatPerson2 { get; set; }
        public string Designation2 { get; set; }
        public string phone2 { get; set; }
        public string ContatPerson3 { get; set; }
        public string Designation3 { get; set; }
        public string phone3 { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public string DevitOrCredit { get; set; }
        public DateTime OpeningDate { get; set; }
        public string FAX { get; set; }
        public string URL { get; set; }
        public string BIN { get; set; }
        public string VatRegNo { get; set; }
        public string Tin { get; set; }
        public string Category { get; set; }
        public string CustomerType { get; set; }
        public string CountryId { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string ZipCode { get; set; }
        public string CompanyCode { get; set; }
    }

    public class PortNameModel
    {
        public string PortNameId { get; set; }
        public string PortName { get; set; }
        public string ShortName { get; set; }
        public string PortId { get; set; }
        public int TC { get; set; }
        public object PortTypeName { get; set; }
    }

    public class ImporterModel
    {
        public string ImporterID { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Address { get; set; }
        public string Remarks { get; set; }
        public string ContactPerson { get; set; }
        public string Designation { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ContactPerson2 { get; set; }
        public string Designation2 { get; set; }
        public string Phone2 { get; set; }
        public string Email2 { get; set; }
        public decimal TC { get; set; }
    }

    public class PlaceOfLoadingModel
    {
        public string PlaceOfLoadingID { get; set; }
        public string PlaceOfLoadingName { get; set; }
        public string ShortName { get; set; }
        public int AutoID { get; set; }
    }

    public class CustomerDeliveryAddressModel
    {
        public string city;

        public string DeliveryLocationCode { get; set; }
        public string CustomerID { get; set; }
        public string LocationAddress { get; set; }
        public string ContactPerson { get; set; }
        public string DesignationCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string CountryId { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string ZipCode { get; set; }
        public string Remarks { get; set; }
        public int AutoId { get; set; }
    }

    public class ShedYardModel
    {
        public string ShedYardID { get; set; }
        public string ShedYardName { get; set; }
        public string ShortName { get; set; }
        public int AutoID { get; set; }
    }
}
