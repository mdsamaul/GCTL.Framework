

namespace GCTL.Core.ViewModels.AddSalesCustomer
{
    //public class CustomerDeliveryReportVM
    //{
    //    //Company Info

    //    public string CompanyName { get; set; }
    //    public string CompanyAddress { get; set; }


    //    // Customer info
    //    public string CustomerID { get; set; }
    //    public string CustomerName { get; set; }
    //    public string CustomerAddress { get; set; }
    //    public string CustomerPhone { get; set; }
    //    public string CustomerEmail { get; set; }
    //    public string CustomerType { get; set; }

    //    // Delivery info
    //    public string DeliveryLocationCode { get; set; }
    //    public string DeliveryAddress { get; set; }
    //    public string DeliveryPhone { get; set; }
    //    public string DeliveryEmail { get; set; }

    //    // Contact person info (flattened)
    //    public string ContactPersonNames { get; set; }           
    //    public string ContactPersonDesignations { get; set; }   
    //    public string ContactPersonPhones { get; set; }         
    //    public string ContactPersonEmails { get; set; }
    //}
    public class CustomerDeliveryReportVM
    {
        // Company Info
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }

        // Customer info
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerType { get; set; }

        public string? FAX { get; set; }
        public string? URL { get; set; }
        public string? BIN { get; set; }

        // List of delivery addresses
        public List<DeliveryDetailVM> DeliveryDetails { get; set; } = new();
    }

    public class DeliveryDetailVM
    {
        public string? DeliveryLocationCode { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryEmail { get; set; }

        public List<ContactPersonVM> ContactPersons { get; set; } = new();
    }

    public class ContactPersonVM
    {
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
