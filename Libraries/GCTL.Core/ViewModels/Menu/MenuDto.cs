using System.Collections.Generic;

namespace GCTL.Core.ViewModels.Menu;

public class MenuDto
{
    public int AutoId { get; set; }
    public string MenuId { get; set; }
    public string Title { get; set; }
    public string ParentId { get; set; }
    public int OrderBy { get; set; }
    public string ControllerName { get; set; }
    public string ViewName { get; set; }
    public string Icon { get; set; }
    public bool IsActive { get; set; }
    public string TableName { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}
