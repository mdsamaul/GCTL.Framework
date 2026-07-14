using GCTL.Core.ViewModels.HolidayLIst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.HolidayLIst
{
    public interface IholidayListService
    {
        Task<List<HolidayDto>> GetHolidayListAsync(int year, int? month = null);
    }
}
