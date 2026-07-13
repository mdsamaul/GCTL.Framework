using GCTL.Core.Data;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.CoreServiceType
{
    public class ServiceTypeService : AppService<GCTL.Data.Models.CoreServiceType>, IServiceType
    {
        private readonly IRepository<GCTL.Data.Models.CoreServiceType> _repository;  
        public ServiceTypeService(IRepository<GCTL.Data.Models.CoreServiceType> genericRepository) : base(genericRepository)
        {
            _repository = genericRepository;
        }

        public async Task<List<GCTL.Data.Models.CoreServiceType>> GetAllCoreServiceAsync()
        {
            var data = await _repository.All()
                .Select(x => new GCTL.Data.Models.CoreServiceType
                {
                    ServiceTypeId = x.ServiceTypeId,
                    ServiceTypeName = x.ServiceTypeName
                })
                .ToListAsync();

            return data;
        }

    }
}

