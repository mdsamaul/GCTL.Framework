using Microsoft.AspNetCore.Mvc;
using GCTL.Service.Menu;
using GCTL.Core.ViewModels.Menu;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers;

public class MenuController : BaseController
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IActionResult> Index()
    {
        // The menu service returns a hierarchical list of MenuDto objects.
        var menu = await _menuService.GetMenuTreeAsync();
        return View(menu);
    }
}
