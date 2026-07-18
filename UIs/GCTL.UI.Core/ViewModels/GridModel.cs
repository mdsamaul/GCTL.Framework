namespace GCTL.UI.Core.ViewModels
{
    public class GridColumn
    {
        public string? Header { get; set; }
        public string? HeaderBangla { get; set; } // optional, for bilingual headers
        public string? CssClass { get; set; } = "text-center";
        public string? Width { get; set; }
    }


    public class GridModel
    {
        public string? GridId { get; set; }              // e.g. "designation-grid"
        public string? CheckAllId { get; set; }           // e.g. "designation-check-all"
        public List<GridColumn>? Columns { get; set; } = new();
        public bool ShowCheckbox { get; set; } = true;
        public bool Striped { get; set; } = true;
        public bool Bordered { get; set; } = false;
    }
}
