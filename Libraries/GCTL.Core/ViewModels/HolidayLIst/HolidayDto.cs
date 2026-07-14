using System;
using System.Collections.Generic;

namespace GCTL.Core.ViewModels.HolidayLIst
{
    public class HolidayDto
    {
        public int Id { get; set; }
        public string HolidayName { get; set; }
        public string HolidayDescription { get; set; }
        public DateTime HolidayDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalDays { get; set; }
        public bool IsActive { get; set; }

        public string Date => HolidayDate.ToString("yyyy-MM-dd");
        public string Name => HolidayName;
    }
}