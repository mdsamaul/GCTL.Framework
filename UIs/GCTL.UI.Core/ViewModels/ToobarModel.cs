namespace GCTL.UI.Core.ViewModels
{
    public class ToobarModel
    {
        public string Title { get; set; } = "";
        public bool ShowTitle { get; set; } = true;

        public bool ShowSave { get; set; } = true;
        public bool ShowDelete { get; set; } = true;
        public bool ShowClear { get; set; } = true;
        public bool ShowPrint { get; set; } = true;
        public bool ShowFavorite { get; set; } = true;

        // ================= Extra / Custom Content Slots =================
        public bool ShowExtraContent1 { get; set; } = false;
        public bool ShowExtraContent2 { get; set; } = false;

        public bool ShowExtraContent
        {
            get => ShowExtraContent1 || ShowExtraContent2;
            set
            {
                ShowExtraContent1 = value;
                ShowExtraContent2 = value;
            }
        }

        // Aliases for convenience
        public bool ShowCustomContent1 { get => ShowExtraContent1; set => ShowExtraContent1 = value; }
        public bool ShowCustomContent2 { get => ShowExtraContent2; set => ShowExtraContent2 = value; }
        public bool ShowCustomContent { get => ShowExtraContent; set => ShowExtraContent = value; }

        public string ExtraContent1 { get; set; } = "";
        public string ExtraContent2 { get; set; } = "";
        public string ExtraContent1Class { get; set; } = "";
        public string ExtraContent2Class { get; set; } = "";
        public string ExtraContent1Style { get; set; } = "";
        public string ExtraContent2Style { get; set; } = "";

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

        public string SaveBtnType { get; set; } = "button";
        public string DeleteBtnType { get; set; } = "button";
        public string ClearBtnType { get; set; } = "button";
        public string PrintBtnType { get; set; } = "button";
        public string FavtBtnType { get; set; } = "button";

        // ================= Custom Styling & Spacing =================
        public string CustomClass { get; set; } = "";
        public string CssClass { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ContainerClass { get; set; } = "";

        public string CustomStyle { get; set; } = "";
        public string Style { get; set; } = "";

        public string Padding { get; set; } = "";
        public string CustomPadding { get; set; } = "";

        public string Margin { get; set; } = "";
        public string CustomMargin { get; set; } = "";

        // ================= Section & Button Specific Customization =================
        public string TitleClass { get; set; } = "";
        public string TitleStyle { get; set; } = "";

        public string ButtonGroupClass { get; set; } = "";
        public string ButtonGroupStyle { get; set; } = "";

        public string SaveBtnStyle { get; set; } = "";
        public string DeleteBtnStyle { get; set; } = "";
        public string ClearBtnStyle { get; set; } = "";
        public string PrintBtnStyle { get; set; } = "";
        public string FavtBtnStyle { get; set; } = "";
    }

    public class ToolbarButton
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string Icon { get; set; } = "";              // "fas fa-plus"
        public string CssClass { get; set; } = "";
        public string Title { get; set; } = "";             // tooltip
        public bool Enabled { get; set; } = true;

        public bool IsDropdown { get; set; } = false;
        public List<ToolbarDropdownItem> DropdownItems { get; set; } = new();

        public string OnClick { get; set; } = "";
        public string Href { get; set; } = "#";
        public bool IsLink { get; set; } = false;
    }

    public class ToolbarDropdownItem
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string Icon { get; set; } = "";
        public string CssClass { get; set; } = "";
        public string Href { get; set; } = "#";
    }
}
