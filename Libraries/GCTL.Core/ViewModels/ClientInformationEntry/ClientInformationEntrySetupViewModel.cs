using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Core.ViewModels.ClientInformationEntry
{
    public class ClientInformationEntrySetupViewModel : BaseViewModel
    {
        public decimal AutoId { get; set; }
        public string? ClientId { get; set; }
        [Required(ErrorMessage = "Client Name is required")]
        public string ClientName { get; set; }
        public string? ShortName { get; set; }
        public string? ClientAddress { get; set; }
        public string? CountryId { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        [RegularExpression(@"^(?:(?:\+|00)88|01)?\d{11}$", ErrorMessage = "Invalid Phone")]
        [MaxLength(14)]
        public string? Phone { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers are allowed")]
        public string? Fax { get; set; }
        [EmailAddress(ErrorMessage = "Please enter valid email")]
        public string? Email { get; set; }
        public string? Url { get; set; }
        public string? Bin { get; set; }
        public string? Vat { get; set; }
        public string? Tin { get; set; }
        public string? ContatPerson1 { get; set; }
        public string? Designation1 { get; set; }
        [RegularExpression(@"^(?:(?:\+|00)88|01)?\d{11}$", ErrorMessage = "Invalid Phone")]
        [MaxLength(14)]
        public string? Phone1 { get; set; }
        [EmailAddress(ErrorMessage = "Please enter valid email")]
        public string? Email1 { get; set; }
        public string? ContatPerson2 { get; set; }
        public string? Designation2 { get; set; }
        [RegularExpression(@"^(?:(?:\+|00)88|01)?\d{11}$", ErrorMessage = "Invalid Phone")]
        [MaxLength(14)]
        public string? Phone2 { get; set; }
        [EmailAddress(ErrorMessage = "Please enter valid email")]
        public string? Email2 { get; set; }
        public string? UserEmployeeId { get; set; }
        public string? CompanyCode { get; set; }
    }
}
