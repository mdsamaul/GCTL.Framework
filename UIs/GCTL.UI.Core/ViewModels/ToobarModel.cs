namespace GCTL.UI.Core.ViewModels
{
    public class ToobarModel
    {
        public string Title { get; set; } = "";

        public bool ShowSave { get; set; } = true;
        public bool ShowDelete { get; set; } = true;
        public bool ShowClear { get; set; } = true;
        public bool ShowPrint { get; set; } = true;
        public bool ShowFavorite { get; set; } = true;

        public bool EnableSave { get; set; } = true;
        public bool EnableDelete { get; set; } = true;
        public bool EnableClear { get; set; } = true;
        public bool EnablePrint { get; set; } = true;
        public bool EnableFavorite { get; set; } = true;

        public string SaveBtnId { get; set; } = "saveBtn";
        public string DeleteBtnId { get; set; } = "deleteBtn";
        public string ClearBtnId { get; set; } = "clearBtn";
        public string PrintBtnId { get; set; } = "printBtn";
        public string FavtBtnId { get; set; } = "favtBtn";

        public string SaveBtnClass { get; set; } = "saveBtn";
        public string DeleteBtnClass { get; set; } = "deleteBtn";
        public string ClearBtnClass { get; set; } = "clearBtn";
        public string PrintBtnClass { get; set; } = "printBtn";
        public string FavtBtnClass { get; set; } = "favtBtn";
        public string ExcelBtnClass { get; set; } = "excelBtn";
        public string PdfBtnClass { get; set; } = "pdfBtn";
        public bool IsPlain { get; set; } = false;
        public bool IsMarginRight { get; set; } = false;
        public bool ReserveActionSlot { get; set; } = true;
    }
}
