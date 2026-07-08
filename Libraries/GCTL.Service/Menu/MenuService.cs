using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Data.Models;
using GCTL.Core.ViewModels.Menu;

namespace GCTL.Service.Menu;

public class MenuService : IMenuService
{
    private readonly IRepository<CoreMenuTab> _menuRepository;

    public MenuService(IRepository<CoreMenuTab> menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<List<MenuDto>> GetMenuTreeAsync()
    {
        var menuTabs = (await _menuRepository.GetAllAsync())
            .OrderBy(m => m.OrderBy)
            .ToList();

        // Build a lookup dictionary by MenuId
        var lookup = menuTabs.ToDictionary(m => m.MenuId, m => new MenuDto
        {
            AutoId = m.AutoId,
            MenuId = m.MenuId,
            Title = m.Title,
            ParentId = m.ParentId,
            OrderBy = m.OrderBy,
            ControllerName = m.ControllerName,
            ViewName = m.ViewName,
            Icon = m.Icon,
            IsActive = true,
            TableName = m.TableName,
            Children = new List<MenuDto>()
        });

        // Assemble hierarchy
        var roots = new List<MenuDto>();
        foreach (var dto in lookup.Values)
        {
            if (string.IsNullOrWhiteSpace(dto.ParentId) || !lookup.ContainsKey(dto.ParentId))
            {
                roots.Add(dto);
            }
            else
            {
                var parent = lookup[dto.ParentId];
                parent.Children.Add(dto);
            }
        }

        // Optionally sort children recursively by OrderBy
        void SortChildren(List<MenuDto> items)
        {
            items.Sort((a, b) => a.OrderBy.CompareTo(b.OrderBy));
            foreach (var child in items)
            {
                if (child.Children != null && child.Children.Any())
                    SortChildren(child.Children);
            }
        }
        SortChildren(roots);

        return roots;
    }
}
