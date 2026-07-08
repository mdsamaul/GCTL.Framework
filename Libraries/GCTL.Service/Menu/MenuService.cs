using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Data.Models;
using GCTL.Core.ViewModels.Menu;
using GCTL.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.Menu;

public class MenuService : IMenuService
{
    private readonly GctlHrmContext _context;

    public MenuService(GctlHrmContext context)
    {
        _context = context;
    }

    public async Task<List<MenuDto>> GetMenuTreeAsync()
    {
        var menuTabs = await _context.CoreMenuTabs
            .AsNoTracking()
            .OrderBy(m => m.OrderBy)
            .ToListAsync();

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
