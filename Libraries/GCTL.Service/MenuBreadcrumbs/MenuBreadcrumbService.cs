using GCTL.Core.Configurations;
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GCTL.Service.MenuBreadcrumbs
{
    public class MenuBreadcrumbService : IMenuBreadcrumbService
    {
        private const string CacheKey = "MenuTab_ActiveList";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IMemoryCache _cache;
        private readonly ApplicationSettings _settings;
        private readonly IRepository<CoreMenuTab> menuRepository;

        public MenuBreadcrumbService(
            IMemoryCache cache, 
            ApplicationSettings settings, 
            IRepository<CoreMenuTab> menuRepository)
        {
            _cache = cache;
            _settings = settings;
            this.menuRepository = menuRepository;
        }
        
        public void InvalidateCache() => _cache.Remove(CacheKey);

        public async Task<List<BreadcrumbItem>> BuildBreadcrumbAsync(string controllerName, string viewName, string fallbackTitle)
        {
            var result = new List<BreadcrumbItem>
            {
                new BreadcrumbItem{Title="Home", Url="/Dashboard", IsActive = true}
            };

            if (string.IsNullOrWhiteSpace(controllerName))
            {
                AppendFallback(result, fallbackTitle, controllerName);
                return result;
            }

            var allMenus = await GetActiveMenusAsync();
            var byMenuId = BuildMenuIdLookup(allMenus);

            var current = allMenus.FirstOrDefault(m =>
                    string.Equals(m.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(viewName) &&
                    string.Equals(m.ViewName, viewName, StringComparison.OrdinalIgnoreCase))
                ?? allMenus.FirstOrDefault(m =>
                    string.Equals(m.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase));

            if (current == null)
            {
                AppendFallback(result, fallbackTitle, controllerName);
                return result;
            }

            var chain = BuildParentChain(current, byMenuId);
            AppendChainToResult(result, chain, byMenuId);

            return result;
        }

        private void AppendChainToResult(
            List<BreadcrumbItem> result,
            List<CoreMenuTab> chain,
            Dictionary<string, CoreMenuTab> byMenuId)
        {
            var urlPath = string.IsNullOrWhiteSpace(_settings.VirtualPath)
                ? ""
                : $"/{_settings.VirtualPath}";

            for (int i = 0; i < chain.Count; i++)
            {
                var m = chain[i];
                bool isLast = i == chain.Count - 1;
                string url;

                if (!string.IsNullOrWhiteSpace(m.ControllerName))
                {
                    url = $"{urlPath}/{m.ControllerName}{(!string.IsNullOrWhiteSpace(m.ViewName) ? "/" + m.ViewName : "")}";
                }
                else if (IsGrandChildDepth(m, byMenuId))
                {
                    url = $"{urlPath}/Menu/GreatGrandChildrenView?grandchildId={Uri.EscapeDataString(m.MenuId)}";
                }
                else
                {
                    url = null;
                }

                result.Add(new BreadcrumbItem { Title = m.Title, Url = url, IsActive = isLast });
            }
        }

        private static bool IsGrandChildDepth(CoreMenuTab node, Dictionary<string, CoreMenuTab> byMenuId)
        {
            if (string.IsNullOrWhiteSpace(node.ParentId) || node.ParentId == "0")
                return false;

            if (!byMenuId.TryGetValue(node.ParentId, out var parent))
                return false;

            if (string.IsNullOrWhiteSpace(parent.ParentId) || parent.ParentId == "0")
                return false;

            if (!byMenuId.TryGetValue(parent.ParentId, out var grandParent))
                return false;

            return grandParent.ParentId == "0";
        }

        private static Dictionary<string, CoreMenuTab> BuildMenuIdLookup(List<CoreMenuTab> allMenus) =>
            allMenus
                .Where(m => !string.IsNullOrWhiteSpace(m.MenuId))
                .ToDictionary(m => m.MenuId, m => m, StringComparer.OrdinalIgnoreCase);

        private static ILookup<string, CoreMenuTab> BuildChildrenLookup(List<CoreMenuTab> allMenus) =>
            allMenus
                .Where(m => !string.IsNullOrWhiteSpace(m.ParentId))
                .ToLookup(m => m.ParentId, m => m, StringComparer.OrdinalIgnoreCase);

        public async Task<List<BreadcrumbItem>> BuildBreadcrumbByMenuIdAsync(string menuId, string fallbackTitle)
        {
            var result = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Title = "Home", Url = "/Dashboard", IsActive = false }
            };

            if (string.IsNullOrWhiteSpace(menuId))
            {
                AppendFallback(result, fallbackTitle, null);
                return result;
            }

            var allMenus = await GetActiveMenusAsync();
            var byMenuId = BuildMenuIdLookup(allMenus);
            //var childrenByParentId = BuildChildrenLookup(allMenus);

            if (!byMenuId.TryGetValue(menuId, out var current))
            {
                AppendFallback(result, fallbackTitle, null);
                return result;
            }

            var chain = BuildParentChain(current, byMenuId);
            AppendChainToResult(result, chain, byMenuId);
            return result;
        }

        private static List<CoreMenuTab> BuildParentChain(CoreMenuTab start, Dictionary<string, CoreMenuTab> byMenuId)
        {
            var chain = new List<CoreMenuTab>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var node = start;

            while (node != null && visited.Add(node.MenuId ?? Guid.NewGuid().ToString()))
            {
                chain.Add(node);

                if (string.IsNullOrWhiteSpace(node.ParentId) || !byMenuId.TryGetValue(node.ParentId, out var parent))
                    break;

                node = parent;
            }

            chain.Reverse();
            return chain;
        }

        private async Task<List<CoreMenuTab>> GetActiveMenusAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<CoreMenuTab> cached))
                return cached;

            var menus = await menuRepository.All().Where(x => x.IsActive).AsNoTracking().ToListAsync();

            _cache.Set(CacheKey, menus, CacheDuration);
            return menus;
        }

        private static void AppendFallback(List<BreadcrumbItem> result, string fallbackTitle, string controllerName)
        {
            var title = !string.IsNullOrWhiteSpace(fallbackTitle)
                ? fallbackTitle
                : (controllerName ?? "Page");

            result.Add(new BreadcrumbItem { Title=title, Url= null, IsActive = true });
        }
    }
}
