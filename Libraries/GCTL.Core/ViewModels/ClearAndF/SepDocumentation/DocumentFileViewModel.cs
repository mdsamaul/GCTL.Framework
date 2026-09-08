using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GCTL.Core.ViewModels.ClearAndF.SepDocumentation
{
    public class DocumentFileViewModel
    {
        public decimal AutoId { get; set; }

        [Required(ErrorMessage = "Job No is required")]
        [Display(Name = "Job No")]
        public string JobNo { get; set; }

        [Display(Name = "Date")]
        public DateTime? DocumentDate { get; set; }

        [Display(Name = "Customer")]
        public string? Customer { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        // Document List
        public List<DocumentItem> Documents { get; set; } = new List<DocumentItem>();

        // This will receive ALL files at once on final submit
        [Required(ErrorMessage = "At least one document is required")]
        public List<IFormFile> UploadFile { get; set; } = new List<IFormFile>();

        // Add this hidden field to pass the temporary client-side metadata
        public string TempDocumentsJson { get; set; }

       
        //[Display(Name = "Upload File")]
        //public IFormFile? UploadFile { get; set; }

        
        [Display(Name = "Document Name")]
        public string? DocumentName { get; set; }

        [Display(Name = "Description")]
        public string? DocumentDescription { get; set; }

        public string? DocumentType { get; set; }

        public DateTime EntryDate { get; set; }
        public DateTime? LastModifyDate { get; set; }
    }

    public class DocumentItem
    {
        public decimal AutoId { get; set; }
        public string DocCode { get; set; }
        public string DocumentName { get; set; }
        public string DocumentDescription { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public bool IsSelected { get; set; }
    }
}
