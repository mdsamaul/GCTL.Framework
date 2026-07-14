using Microsoft.AspNetCore.Mvc;
using GCTL.Service.Menu;
using System.Threading.Tasks;

namespace GCTL.UI.Core.ViewComponents;

public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public MenuViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var menu = await _menuService.GetMenuTreeAsync();
        // Even if menu is empty, we still render the partial to keep the sidebar visible.
        return View(menu);
    }
}
