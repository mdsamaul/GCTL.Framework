using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.CustomerRelationshipManagement.AddSalesDeliveryLocation
{
    public class SalesDeliveryLocationService : AppService<SalesDeliveryLocation>, ISalesDeliveryLocationService
    {
        private readonly IRepository<SalesCustomer> _salesCustomer;
        private readonly IRepository<SalesContactPerson> _salesContactRepository;

        private readonly IRepository<SalesDeliveryLocation> _salesDeliveryLocation;

        public SalesDeliveryLocationService(IRepository<SalesDeliveryLocation> salesDeliveryLocation, IRepository<SalesCustomer> salesCustomer, IRepository<SalesContactPerson> salesContactRepository) : base(salesDeliveryLocation)
        {
            _salesCustomer = salesCustomer;
            _salesDeliveryLocation = salesDeliveryLocation;
            _salesContactRepository = salesContactRepository;
        }

        public async Task<PaginationResponse<SalesDeliveryLocationViewModel>> GetPaginatedAsync(PaginationRequest request, string customerId = "")
        {
            var deliveryLocationsQuery = _salesDeliveryLocation.All().AsNoTracking();
            var customersQuery = _salesCustomer.All().AsNoTracking();

            var totalUnfilteredCount = await deliveryLocationsQuery.CountAsync();

            var query = from dl in deliveryLocationsQuery
                        join c in customersQuery on dl.CustomerId equals c.CustomerId into cg
                        from c in cg.DefaultIfEmpty()
                        where string.IsNullOrEmpty(customerId) || dl.CustomerId == customerId
                        select new SalesDeliveryLocationViewModel
                        {
                            DeliveryLocationCode = dl.DeliveryLocationCode,
                            CustomerId = dl.CustomerId,
                            CustomerName = c != null ? c.CustomerName : "",
                            LocationAddress = dl.LocationAddress ?? "",
                            CountryId = dl.CountryId ?? "",
                            City = dl.City ?? "",
                            StateOrProvince = dl.StateOrProvince ?? "",
                            ZipCode = dl.ZipCode ?? "",
                            Phone = dl.Phone ?? "",
                            Email = dl.Email ?? "",
                            ContactPersonName = "",
                            Remarks = dl.Remarks ?? "",
                            ContactPerson = dl.ContactPerson ?? ""
                        };

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn) || request.SortColumn.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                request.SortColumn = "DeliveryLocationCode";
                request.SortOrder = "desc";
            }

            var paginatedResult = await PaginationService<SalesDeliveryLocationViewModel>.GetPaginatedData(query, request);
            paginatedResult.TotalCount = totalUnfilteredCount;

            if (paginatedResult.Data != null && paginatedResult.Data.Any())
            {
                var deliveryLocationCodes = paginatedResult.Data.Select(x => x.DeliveryLocationCode).ToList();
                var relevantDeliveryLocations = await _salesDeliveryLocation.All().AsNoTracking()
                    .Where(dl => deliveryLocationCodes.Contains(dl.DeliveryLocationCode))
                    .ToListAsync();
                var allContacts = await _salesContactRepository.All().AsNoTracking().ToListAsync();

                var contactPersonMap = relevantDeliveryLocations
                    .Where(dl => !string.IsNullOrEmpty(dl.ContactPerson))
                    .SelectMany(dl => dl.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(cpId => {
                            var contact = allContacts.FirstOrDefault(c => c.Cpid != null && c.Cpid.Trim() == cpId.Trim());
                            return new { dl.DeliveryLocationCode, ContactName = contact?.ContactPersonName ?? "" };
                        }))
                    .GroupBy(x => x.DeliveryLocationCode)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.ContactName).Where(name => !string.IsNullOrEmpty(name))));

                foreach (var item in paginatedResult.Data)
                {
                    if (item.DeliveryLocationCode != null && contactPersonMap.ContainsKey(item.DeliveryLocationCode))
                    {
                        item.ContactPersonName = contactPersonMap[item.DeliveryLocationCode];
                    }
                }
            }

            return paginatedResult;
        }

        public async Task<bool> DeleteAsyncDeliveryLocation(string deliveryId)
        {
            var location = await _salesDeliveryLocation.GetByIdAsync(deliveryId);
            if (location == null)
            { return false; }

            await _salesDeliveryLocation.DeleteAsync(deliveryId);
            return true;
        }

        public async Task<string> GenerateDeliveryLocationIdAsync()
        {
            var lastDL = await _salesDeliveryLocation.All()
            .OrderByDescending(c => c.DeliveryLocationCode)
            .FirstOrDefaultAsync();

            // Handle the case where no customers exist
            string newDLId = lastDL?.DeliveryLocationCode;


            return newDLId;
        }

        public async Task<List<SalesDeliveryLocationViewModel>> GetAllAsync()
        {
            var customers = await _salesCustomer.All()
                .Select(c => new { c.CustomerId, c.CustomerName })
                .ToListAsync();


            var deliveryLocations = await _salesDeliveryLocation.All()
                .Select(dl => new SalesDeliveryLocationViewModel
                {
                    DeliveryLocationCode = dl.DeliveryLocationCode,
                    CustomerId = dl.CustomerId,
                    LocationAddress = dl.LocationAddress ?? "",
                    CountryId = dl.CountryId ?? "",
                    City = dl.City ?? "",
                    StateOrProvince = dl.StateOrProvince ?? "",
                    ZipCode = dl.ZipCode ?? "",
                    Phone = dl.Phone ?? "",
                    Email = dl.Email ?? "",
                    ContactPerson = dl.ContactPerson ?? "",
                    Remarks = dl.Remarks ?? ""
                }).ToListAsync();

            // Map customer name to the delivery location
            foreach (var location in deliveryLocations)
            {
                var customer = customers.FirstOrDefault(c => c.CustomerId == location.CustomerId);
                location.CustomerName = customer?.CustomerName ?? "Unknown"; // Add CustomerName field dynamically
            }

            return deliveryLocations;
        }

        public async Task<SalesDeliveryLocationViewModel> GetByIdAsync(string id)
        {
            var data = await _salesDeliveryLocation.All()
            .Where(dl => dl.DeliveryLocationCode == id)
            .Select(dl => new SalesDeliveryLocationViewModel
            {
                DeliveryLocationCode = dl.DeliveryLocationCode,
                CustomerId = dl.CustomerId,
                LocationAddress = dl.LocationAddress ?? "",
                CountryId = dl.CountryId ?? "",
                City = dl.City ?? "",
                StateOrProvince = dl.StateOrProvince ?? "",
                ZipCode = dl.ZipCode ?? "",
                Phone = dl.Phone ?? "",
                Email = dl.Email ?? "",
                ContactPerson = dl.ContactPerson ?? "",
                Remarks = dl.Remarks ?? ""
            }).FirstOrDefaultAsync();

            return data;
        }

        public async Task<List<SalesDeliveryLocationViewModel>> GetDeliveryLocationByCustomer(string cusId)
        {
            var data = await (from dl in _salesDeliveryLocation.All()
                              join c in _salesCustomer.All() on dl.CustomerId equals c.CustomerId
                              where dl.CustomerId == cusId
                              select new SalesDeliveryLocationViewModel
                              {
                                  DeliveryLocationCode = dl.DeliveryLocationCode,
                                  CustomerId = dl.CustomerId,
                                  CustomerName = c.CustomerName,
                                  LocationAddress = dl.LocationAddress ?? "",
                                  CountryId = dl.CountryId ?? "",
                                  City = dl.City ?? "",
                                  StateOrProvince = dl.StateOrProvince ?? "",
                                  ZipCode = dl.ZipCode ?? "",
                                  Phone = dl.Phone ?? "",
                                  Email = dl.Email ?? "",
                                  ContactPerson = dl.ContactPerson ?? "",
                                  Remarks = dl.Remarks ?? ""
                              }).ToListAsync();
            return data;
        }


        public async Task<PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>> GetPaginatedDeliveryLocations(string customerId = "", int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "DeliveryLocationCode", string sortOrder = "desc")
        {
            try
            {        // Keep as IQueryable - don't materialize yet
                var deliveryLocationsQuery = _salesDeliveryLocation.All();
                var customersQuery = _salesCustomer.All();
                var contactsQuery = _salesContactRepository.All();
                // Build the main query with joins
                var deliveryLocationQuery = from dl in deliveryLocationsQuery
                                            join c in customersQuery on dl.CustomerId equals c.CustomerId
                                            where string.IsNullOrEmpty(customerId) || dl.CustomerId == customerId
                                            select new SalesDeliveryLocationViewModel
                                            {
                                                DeliveryLocationCode = dl.DeliveryLocationCode,
                                                CustomerId = dl.CustomerId,
                                                CustomerName = c.CustomerName,
                                                LocationAddress = dl.LocationAddress ?? "",
                                                CountryId = dl.CountryId ?? "",
                                                City = dl.City ?? "",
                                                StateOrProvince = dl.StateOrProvince ?? "",
                                                ZipCode = dl.ZipCode ?? "",
                                                Phone = dl.Phone ?? "",
                                                Email = dl.Email ?? "",
                                                ContactPersonName = "", // Will be populated after pagination
                                                Remarks = dl.Remarks ?? ""
                                            };
                if (pageSize == -1)
                {
                    pageSize = await deliveryLocationQuery.CountAsync();
                    pageNumber = 1;
                }

                var paginatedResult = await PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.GetPaginatedData(deliveryLocationQuery, pageNumber, pageSize, searchTerm, sortColumn, sortOrder, term => dl => EF.Functions.Like(dl.LocationAddress ?? "", $"%{term}%") || EF.Functions.Like(dl.DeliveryLocationCode ?? "", $"%{term}%"), dl => dl);        // Now populate contact person names for only the paginated results
                if (paginatedResult.Data.Any())
                {            // Get delivery location codes from paginated results
                    var deliveryLocationCodes = paginatedResult.Data.Select(x => x.DeliveryLocationCode).ToList();            // Get delivery locations and contacts for only these codes
                    var relevantDeliveryLocations = await _salesDeliveryLocation.All().Where(dl => deliveryLocationCodes.Contains(dl.DeliveryLocationCode)).ToListAsync(); var allContacts = await contactsQuery.ToListAsync();            // Build contact person map for only the relevant delivery locations
                    var contactPersonMap = relevantDeliveryLocations.Where(dl => !string.IsNullOrEmpty(dl.ContactPerson)).SelectMany(dl => dl.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(cpId => { var contact = allContacts.FirstOrDefault(c => c.Cpid.Trim() == cpId.Trim()); return new { dl.DeliveryLocationCode, ContactName = contact?.ContactPersonName ?? "" }; })).GroupBy(x => x.DeliveryLocationCode).ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.ContactName).Where(name => !string.IsNullOrEmpty(name))));            // Update contact person names in the paginated results
                    foreach (var item in paginatedResult.Data) { if (contactPersonMap.ContainsKey(item.DeliveryLocationCode)) { item.ContactPersonName = contactPersonMap[item.DeliveryLocationCode]; } }
                }
                return paginatedResult;
            }
            catch (Exception ex) { Console.WriteLine($"Error in GetPaginatedDeliveryLocations: {ex.Message}"); return new PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel> { Data = new List<SalesDeliveryLocationViewModel>(), TotalCount = 0 }; }
        }



        //    public async Task<PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>> GetPaginatedDeliveryLocations(
        //string customerId = "",
        //int pageNumber = 1,
        //int pageSize = 10,
        //string searchTerm = "",
        //string sortColumn = "DeliveryLocationCode",
        //string sortOrder = "desc")
        //    {
        //        try
        //        {
        //            var deliveryLocations = await _salesDeliveryLocation.All().ToListAsync();
        //            var customers = await _salesCustomer.All().ToListAsync();
        //            var contacts = await _salesContactRepository.All().ToListAsync();

        //            // Build dictionary of DeliveryLocationCode -> ContactPerson Names
        //            var contactPersonMap = deliveryLocations
        //                .SelectMany(dl =>
        //                    string.IsNullOrEmpty(dl.ContactPerson)
        //                        ? new[] { new { dl.DeliveryLocationCode, ContactName = "" } }
        //                        : dl.ContactPerson
        //                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                            .Select(cpId =>
        //                            {
        //                                var contact = contacts.FirstOrDefault(c => c.CPID.Trim() == cpId.Trim());
        //                                return new
        //                                {
        //                                    dl.DeliveryLocationCode,
        //                                    ContactName = contact?.ContactPersonName ?? ""
        //                                };
        //                            }))
        //                .GroupBy(x => x.DeliveryLocationCode)
        //                .ToDictionary(
        //                    g => g.Key,
        //                    g => string.Join(", ", g.Select(x => x.ContactName).Where(name => !string.IsNullOrEmpty(name)))
        //                );

        //            // Build the view models
        //            var deliveryLocationQuery =  (from dl in deliveryLocations
        //                                         join c in customers on dl.CustomerID equals c.CustomerID
        //                                         where string.IsNullOrEmpty(customerId) || dl.CustomerID == customerId
        //                                         select new SalesDeliveryLocationViewModel
        //                                         {
        //                                             DeliveryLocationCode = dl.DeliveryLocationCode,
        //                                             CustomerId = dl.CustomerID,
        //                                             CustomerName = c.CustomerName,
        //                                             LocationAddress = dl.LocationAddress ?? "",
        //                                             CountryId = dl.CountryId ?? "",
        //                                             City = dl.city ?? "",
        //                                             StateOrProvince = dl.StateOrProvince ?? "",
        //                                             ZipCode = dl.ZipCode ?? "",
        //                                             Phone = dl.Phone ?? "",
        //                                             Email = dl.Email ?? "",
        //                                             ContactPersonName = contactPersonMap.ContainsKey(dl.DeliveryLocationCode)
        //                                                ? contactPersonMap[dl.DeliveryLocationCode]
        //                                                : "",
        //                                             Remarks = dl.Remarks ?? ""
        //                                         }).AsQueryable();

        //            // Paginate
        //            var paginatedResult =  PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.GetPaginatedData(
        //                deliveryLocationQuery,
        //                pageNumber,
        //                pageSize,
        //                searchTerm,
        //                sortColumn,
        //                sortOrder,
        //                term => dl => EF.Functions.Like(dl.LocationAddress ?? "", $"%{term}%") ||
        //                              EF.Functions.Like(dl.DeliveryLocationCode ?? "", $"%{term}%"),
        //                dl => dl);

        //            return await paginatedResult;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error in GetPaginatedDeliveryLocations: {ex.Message}");
        //            return new PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>
        //            {
        //                Data = new List<SalesDeliveryLocationViewModel>(),
        //                TotalCount = 0
        //            };
        //        }
        //    }

        //public async Task<PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>> GetPaginatedDeliveryLocations(string customerId ="",int pageNumber = 1,int pageSize = 10,string searchTerm = "",string sortColumn = "DeliveryLocationCode",string sortOrder = "desc")
        //{
        //    try
        //    {
        //        //var deliveryLocationQuery = (from dl in _salesDeliveryLocation.All()
        //        //                             join c in _salesCustomer.All()
        //        //                                on dl.CustomerID equals c.CustomerID


        //        //                             where string.IsNullOrEmpty(customerId) || dl.CustomerID == customerId
        //        //                             select new SalesDeliveryLocationViewModel
        //        //                             {
        //        //                                 DeliveryLocationCode = dl.DeliveryLocationCode,
        //        //                                 CustomerId = dl.CustomerID,
        //        //                                 CustomerName = c.CustomerName,
        //        //                                 LocationAddress = dl.LocationAddress ?? "",
        //        //                                 CountryId = dl.CountryId ?? "",
        //        //                                 City = dl.city ?? "",
        //        //                                 StateOrProvince = dl.StateOrProvince ?? "",
        //        //                                 ZipCode = dl.ZipCode ?? "",
        //        //                                 Phone = dl.Phone ?? "",
        //        //                                 Email = dl.Email ?? "",
        //        //                                 ContactPerson = dl.ContactPerson ?? "",
        //        //                                 Remarks = dl.Remarks ?? ""
        //        //                             }).AsQueryable();

        //        var deliveryLocationData = await (from dl in _salesDeliveryLocation.All()
        //                                          join c in _salesCustomer.All()
        //                                          on dl.CustomerID equals c.CustomerID
        //                                          where string.IsNullOrEmpty(customerId) || dl.CustomerID == customerId
        //                                          select new { dl, c }).ToListAsync();

        //        var contacts = await _salesContactRepository.All().ToListAsync();
        //        // var contactName = "abc";

        //        //var contactName =
        //        //    from x in deliveryLocationData
        //        //    from cpId in string.IsNullOrEmpty(x.dl.ContactPerson)
        //        //                    ? new string[] { null } 
        //        //                    : x.dl.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries)
        //        //    join contact in contacts
        //        //        on (cpId ?? "").Trim() equals contact.CPID into contactGroup
        //        //    from contact in contactGroup.DefaultIfEmpty()
        //        //    select new SalesDeliveryLocationViewModel
        //        //    {
        //        //        ContactPersonName = contact?.ContactPersonName ?? ""
        //        //    };

        //        //var deliveryLocationQuery = (from dl in _salesDeliveryLocation.All()
        //        //                             join c in _salesCustomer.All()
        //        //                                on dl.CustomerID equals c.CustomerID


        //        //                             where string.IsNullOrEmpty(customerId) || dl.CustomerID == customerId
        //        //                             select new SalesDeliveryLocationViewModel
        //        //                             {
        //        //                                 DeliveryLocationCode = dl.DeliveryLocationCode,
        //        //                                 CustomerId = dl.CustomerID,
        //        //                                 CustomerName = c.CustomerName,
        //        //                                 LocationAddress = dl.LocationAddress ?? "",
        //        //                                 CountryId = dl.CountryId ?? "",
        //        //                                 City = dl.city ?? "",
        //        //                                 StateOrProvince = dl.StateOrProvince ?? "",
        //        //                                 ZipCode = dl.ZipCode ?? "",
        //        //                                 Phone = dl.Phone ?? "",
        //        //                                 Email = dl.Email ?? "",
        //        //                                 ContactPersonName = contactName,
        //        //                                 Remarks = dl.Remarks ?? ""
        //        //                             }).AsQueryable();

        //        // First query: Get contact names for each delivery location
        //        var contactName = from dl in _salesDeliveryLocation.All()
        //                          from cpId in string.IsNullOrEmpty(dl.ContactPerson)
        //                                      ? new string[] { null }
        //                                      : dl.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                          join contact in contacts
        //                              on (cpId ?? "").Trim() equals contact.CPID into contactGroup
        //                          from contact in contactGroup.DefaultIfEmpty()
        //                          group contact.ContactPersonName ?? "" by dl.DeliveryLocationCode into g
        //                          select new
        //                          {
        //                              DeliveryLocationCode = g.Key,
        //                              ContactPersonNames = string.Join(", ", g.Where(name => !string.IsNullOrEmpty(name)))
        //                          };

        //        // Second query: Combine delivery locations with customers and contact names
        //        var deliveryLocationQuery = (from dl in _salesDeliveryLocation.All()
        //                                     join c in _salesCustomer.All()
        //                                         on dl.CustomerID equals c.CustomerID
        //                                     join cn in contactName
        //                                         on dl.DeliveryLocationCode equals cn.DeliveryLocationCode into contactGroup
        //                                     from cn in contactGroup.DefaultIfEmpty()
        //                                     where string.IsNullOrEmpty(customerId) || dl.CustomerID == customerId
        //                                     select new SalesDeliveryLocationViewModel
        //                                     {
        //                                         DeliveryLocationCode = dl.DeliveryLocationCode,
        //                                         CustomerId = dl.CustomerID,
        //                                         CustomerName = c.CustomerName,
        //                                         LocationAddress = dl.LocationAddress ?? "",
        //                                         CountryId = dl.CountryId ?? "",
        //                                         City = dl.city ?? "", // Fixed typo from dl.city to dl.City
        //                                         StateOrProvince = dl.StateOrProvince ?? "",
        //                                         ZipCode = dl.ZipCode ?? "",
        //                                         Phone = dl.Phone ?? "",
        //                                         Email = dl.Email ?? "",
        //                                         ContactPersonName = cn.ContactPersonNames ?? "",
        //                                         Remarks = dl.Remarks ?? ""
        //                                     }).AsQueryable();

        //        var queryableData =  deliveryLocationQuery.AsQueryable();

        //        var paginatedResult = PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.GetPaginatedData(
        //            queryableData,
        //            pageNumber,
        //            pageSize,
        //            searchTerm,
        //            sortColumn,
        //            sortOrder,
        //            term => dl => EF.Functions.Like(dl.LocationAddress ?? "", $"%{term}%") ||
        //                         EF.Functions.Like(dl.DeliveryLocationCode ?? "", $"%{term}%"),
        //            dl => dl);

        //        return await paginatedResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error in GetPaginatedDeliveryLocations: {ex.Message}");
        //        return new PaginationService<SalesDeliveryLocationViewModel, SalesDeliveryLocationViewModel>.PaginationResult<SalesDeliveryLocationViewModel>
        //        {
        //            Data = new List<SalesDeliveryLocationViewModel>(),
        //            TotalCount = 0
        //        };
        //    }
        //}

        public Task<bool> IsExistAsync(string deliveryId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveAsyncDeliveryLocation(SalesDeliveryLocationViewModel deliveryLocation)
        {
            try
            {
                await _salesDeliveryLocation.AddAsync(new SalesDeliveryLocation
                {
                    DeliveryLocationCode = deliveryLocation.DeliveryLocationCode,
                    CustomerId = deliveryLocation.CustomerId,
                    LocationAddress = deliveryLocation.LocationAddress,
                    CountryId = deliveryLocation.CountryId ?? "",
                    City = deliveryLocation.City ?? "",
                    StateOrProvince = deliveryLocation.StateOrProvince ?? "",
                    ZipCode = deliveryLocation.ZipCode ?? "",
                    Phone = deliveryLocation.Phone ?? "",
                    Email = deliveryLocation.Email ?? "",
                    ContactPerson = deliveryLocation.ContactPerson ?? "",
                    Remarks = deliveryLocation.Remarks ?? "",
                    DesignationCode = deliveryLocation.DesignationCode ?? "",
                    DeliveryCode = deliveryLocation.DeliveryCode ?? ""
                });


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating delivery location: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsyncDeliveryLocation(SalesDeliveryLocationViewModel model)
        {
            await _salesDeliveryLocation.BeginTransactionAsync();
            try
            {
                var entity = await _salesDeliveryLocation.GetByIdAsync(model.DeliveryLocationCode);
                if (entity == null)
                {
                    await _salesDeliveryLocation.RollbackTransactionAsync();
                    return false;
                }

                entity.DeliveryLocationCode = model.DeliveryLocationCode;
                entity.CustomerId = model.CustomerId ?? entity.CustomerId;
                entity.LocationAddress = model.LocationAddress ?? "";
                entity.CountryId = model.CountryId ?? "";
                entity.City = model.City ?? "";
                entity.StateOrProvince = model.StateOrProvince ?? "";
                entity.ZipCode = model.ZipCode ?? "";
                entity.Phone = model.Phone ?? "";
                entity.Email = model.Email ?? "";
                entity.ContactPerson = model.ContactPerson ?? "";
                entity.Remarks = model.Remarks ?? "";
                entity.DesignationCode = model.DesignationCode ?? "";
                entity.DeliveryCode = model.DeliveryCode ?? "";

                await _salesDeliveryLocation.UpdateAsync(entity);
                await _salesDeliveryLocation.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _salesDeliveryLocation.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> BulkDeleteAsync(List<string> deliveryIds)
        {
            await _salesDeliveryLocation.BeginTransactionAsync();

            try
            {
                var deliveryLocations = await _salesDeliveryLocation.All()
                    .Where(dl => deliveryIds.Contains(dl.DeliveryLocationCode))
                    .ToListAsync();

                if (deliveryLocations == null || deliveryLocations.Count == 0)
                {
                    await _salesDeliveryLocation.RollbackTransactionAsync();
                    return false;
                }

                await _salesDeliveryLocation.DeleteRangeAsync(deliveryLocations);

                await _salesDeliveryLocation.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _salesDeliveryLocation.RollbackTransactionAsync();
                Console.WriteLine($"Bulk delete delivery location error: {ex}");
                return false;
            }
        }
    }
}
