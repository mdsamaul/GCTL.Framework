using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Data.Models;
using GCTL.Core.ViewModels.Menu;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.Menu;

public interface IMenuService
{
    Task<List<MenuDto>> GetMenuTreeAsync();
}
