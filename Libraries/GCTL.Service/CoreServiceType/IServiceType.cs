using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Data.Models;

namespace GCTL.Service.CoreServiceType
{
    public interface IServiceType
    {
        Task<List<GCTL.Data.Models.CoreServiceType>> GetAllCoreServiceAsync();
    }
}
