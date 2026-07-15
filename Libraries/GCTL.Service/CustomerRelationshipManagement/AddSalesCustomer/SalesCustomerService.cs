using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer
{
    public class SalesCustomerService : AppService<SalesCustomer>, ISalesCustomerService
    {
        private readonly IRepository<SalesCustomer> _salesCustomer;
        private readonly IRepository<SalesDeliveryLocation> _salesDeliveryLocation;
        private readonly IRepository<SalesContactPerson> _contactPerson;
        private readonly IRepository<CoreCountry> coreCountry;
        private readonly IConfiguration _configuration;
        public SalesCustomerService(IRepository<SalesCustomer> salesCustomer, IRepository<SalesDeliveryLocation> salesDeliveryLocation, IRepository<SalesContactPerson> contactPerson, IRepository<CoreCountry> coreCountry, IConfiguration configuration) : base(salesCustomer)
        {
            _salesCustomer = salesCustomer;
            _salesDeliveryLocation = salesDeliveryLocation;
            _contactPerson = contactPerson;
            this.coreCountry = coreCountry;
            _configuration = configuration;
        }

        public async Task<List<CoreCountry>> GetCountryDropdownAsync()
        {
            var country = await coreCountry.All().Select(e => new CoreCountry { CountryId = e.CountryId, CountryName = e.CountryName }).ToListAsync();
            Console.WriteLine(country);

            return await coreCountry.All().Select(e => new CoreCountry { CountryId = e.CountryId, CountryName = e.CountryName }).ToListAsync();
        }


        public async Task<bool> DeleteAsyncSalesCustomer(string customerId)
        {
            await _salesCustomer.BeginTransactionAsync();
            try
            {
                var customer = await _salesCustomer.GetByIdAsync(customerId);
                if (customer == null)
                {
                    await _salesCustomer.RollbackTransactionAsync();
                    return false;
                }
                // Find and delete associated delivery locations first
                var deliveryLocations = await _salesDeliveryLocation.All().Where(x => x.CustomerId == customerId).ToListAsync();
                if (deliveryLocations.Any()) // Check if there are delivery locations
                {
                    await _salesDeliveryLocation.DeleteRangeAsync(deliveryLocations);
                }

                // Now delete the customer
                await _salesCustomer.DeleteAsync(customerId);

                await _salesCustomer.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting customer: {ex}");
                await _salesCustomer.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<SalesCustomerViewModel> GetByIdAsync(string id)
        {
            var customer = _salesCustomer.All();

            //var deliveryAddress = salesDeliveryLocation.All();

            //var deliveryLocations = await salesDeliveryLocation.All()
            //    .Where(dl => dl.CustomerId == id)
            //    .Select(dl => new SalesDeliveryLocationViewModel
            //    {
            //        CustomerId = dl.CustomerId,
            //        DeliveryLocationCode = dl.DeliveryLocationCode,
            //        LocationAddress = dl.LocationAddress,
            //        ContactPerson = dl.ContactPerson,
            //        Phone = dl.Phone,
            //        Email = dl.Email,
            //        CountryId = dl.CountryId,
            //        City = dl.City,
            //        StateOrProvince = dl.StateOrProvince,
            //        ZipCode = dl.ZipCode,
            //        Remarks = dl.Remarks
            //    }).ToListAsync();

            var data = await _salesCustomer.All()
                .Where(sc => sc.CustomerId == id)
                .Select(sc => new SalesCustomerViewModel
                {
                    CustomerId = sc.CustomerId,
                    CustomerName = sc.CustomerName,
                    CustomerCode = sc.CustomerCode,
                    ShortName = sc.ShortName,
                    CustomerAddress = sc.CustomerAddress,
                    CountryId = sc.CountryId,
                    City = sc.City,
                    StateOrProvince = sc.StateOrProvince,
                    ZipCode = sc.ZipCode,
                    Phone = sc.Phone,
                    Email = sc.Email,
                    Url = sc.Url,
                    Fax = sc.Fax,
                    Bin = sc.Bin,
                    Tin = sc.Tin,
                    VatRegNo = sc.VatRegNo,
                    ContactPerson = sc.ContactPerson,
                    OpeningBalance = sc.OpeningBalance ?? 0,
                    OpeningDate = sc.OpeningDate,
                    CreditLimit = sc.CreditLimit,
                    Category = sc.Category,
                    CustomerType = sc.CustomerType,
                    SalesPerson = sc.SalesPersonId,
                    LDate = sc.Ldate,
                    ModifyDate = sc.ModifyDate
                }).FirstOrDefaultAsync();
            return data;

        }

    

        public async Task<PaginationService<SalesCustomer, SalesCustomerViewModel>.PaginationResult<SalesCustomerViewModel>> GetPaginatedSalesCustomer(int pageNumber = 1,int pageSize = 10, string searchTerm = "", string sortColumn = "CustomerID", string sortOrder = "desc")
        {
            try
            {
                var customerQuery = _salesCustomer.All();
                if (pageSize == -1)
                {
                    pageSize = await customerQuery.CountAsync();
                    pageNumber = 1;
                }

                var paginatedResult = await PaginationService<SalesCustomer, SalesCustomerViewModel>.GetPaginatedData(
                    customerQuery,
                    pageNumber,
                    pageSize,
                    searchTerm,
                    sortColumn,
                    sortOrder,
                    term => sc => EF.Functions.Like(sc.CustomerName ?? "", $"%{term}%") ||
                                  EF.Functions.Like(sc.CustomerId ?? "", $"%{term}%") ||
                                  EF.Functions.Like(sc.CustomerAddress ?? "", $"%{term}%") ||
                                  EF.Functions.Like(sc.OpeningBalance.ToString(), $"%{searchTerm}%") ||  
                                  EF.Functions.Like(sc.ShortName ?? "", $"{term}%"),
                    sc => new SalesCustomerViewModel
                    {
                        CustomerId = sc.CustomerId,
                        CustomerName = sc.CustomerName ?? "",
                        ShortName = sc.ShortName ?? "",
                        CustomerAddress = sc.CustomerAddress ?? "",
                        OpeningBalance = sc.OpeningBalance ?? 0.00m,
                    });

                return paginatedResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPaginatedSalesCustomers: {ex.Message}");

                return new PaginationService<SalesCustomer, SalesCustomerViewModel>.PaginationResult<SalesCustomerViewModel>
                {
                    Data = new List<SalesCustomerViewModel>(),
                    TotalCount = 0
                };
            }
        }

        public async Task<bool> IsExistAsync(string customerId)
        {
            return await _salesCustomer.All().AnyAsync(x => x.CustomerId == customerId);
        }

        public async Task<bool> SaveAsyncSalesCustomer(SalesCustomerViewModel model)
        {
            try
            {
                var entity = new SalesCustomer
                {
                    CustomerId = model.CustomerId,
                    CustomerName = model.CustomerName ?? "",
                    CustomerCode = model.CustomerCode ?? "",
                    ShortName = model.ShortName ?? "",
                    CustomerAddress = model.CustomerAddress ?? " ",
                    CountryId = model.CountryId ?? "",
                    City = model.City ?? "",
                    StateOrProvince = model.StateOrProvince ?? "",
                    ZipCode = model.ZipCode ?? "",
                    Phone = model.Phone ?? "",
                    Fax = model.Fax ?? "",
                    Email = model.Email ?? "",
                    Url = model.Url ?? "",
                    Bin = model.Bin ?? "",
                    VatRegNo = model.VatRegNo ?? "",
                    Tin = model.Tin ?? "",
                    ContactPerson = model.ContactPerson ?? "",
                    OpeningBalance = model.OpeningBalance ?? 0,
                    OpeningDate = model.OpeningDate ?? new DateTime(1900, 1, 1),
                    CreditLimit = model.CreditLimit ?? 0,
                    CustomerType = model.CustomerType ?? "",
                    Category = model.Category ?? "",
                    SalesPersonId = model.SalesPerson ?? "",
                    CompanyCode = " ",
                    EmployeeId = " ",
                    Ldate = DateTime.Now,
                    //Others Property For Saving Empty

                    ContatPerson1 = "",
                    ContatPerson2 = "",
                    ContatPerson3 = "",
                    Designation1 = "",
                    Designation2 = "",
                    Designation3 = "",
                    Phone1 = "",
                    Phone2 = "",
                    Phone3 = "",
                    Email1 = "",
                    Email2 = "",
                    Email3 = "",
                    Lip = "",
                    Lmac = "",
                    DevitOrCredit = "",
                    CustomerCompany =""

                };

                await _salesCustomer.AddAsync(entity);

                //var deliveryEntities = model.DeliveryLocations.Select(loc => new SalesDeliveryLocation
                //{
                //    DeliveryLocationCode = loc.DeliveryLocationCode,
                //    CustomerId = model.CustomerId,
                //    LocationAddress = loc.LocationAddress,
                //    ContactPerson = loc.ContactPerson,

                //});

                //await salesDeliveryLocation.AddRangeAsync(deliveryEntities);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsyncSalesCustomer(SalesCustomerViewModel model)
        {
            await _salesCustomer.BeginTransactionAsync();
            try
            {
                var entity = await _salesCustomer.GetByIdAsync(model.CustomerId);
                if (entity == null)
                {
                    await _salesCustomer.RollbackTransactionAsync();
                    return false;
                }
                entity.CustomerId = model.CustomerId;
                entity.CustomerName = model.CustomerName ?? "";
                entity.CustomerCode = model.CustomerCode ?? "";
                entity.ShortName = model.ShortName ?? "";
                entity.CustomerAddress = model.CustomerAddress ?? "";
                entity.CountryId = model.CountryId ?? "";
                entity.City = model.City ?? "";
                entity.StateOrProvince = model.StateOrProvince ?? "";
                entity.ZipCode = model.ZipCode ?? "";
                entity.Phone = model.Phone ?? "";
                entity.Fax = model.Fax ?? "";
                entity.Email = model.Email ?? "";
                entity.Url = model.Url ?? "";
                entity.Bin = model.Bin ?? "";
                entity.VatRegNo = model.VatRegNo ?? "";
                entity.Tin = model.Tin ?? "";
                entity.ContactPerson = model.ContactPerson ?? "";
                entity.OpeningBalance = model.OpeningBalance ?? 0.00m;
                entity.OpeningDate = model.OpeningDate;
                entity.CreditLimit = model.CreditLimit;
                entity.CustomerType = model.CustomerType ?? "";
                entity.Category = model.Category ?? "";
                entity.SalesPersonId = model.SalesPerson ?? "";
                entity.ModifyDate = DateTime.Now;
                await _salesCustomer.UpdateAsync(entity);

                await _salesCustomer.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _salesCustomer.RollbackTransactionAsync();
                return false;
            }
        }

        //public async Task<List<SalesCustomerViewModel>> GetAllAsync()
        //{
        //    //var customer = salesCustomer.All();

        //    var data = await _salesCustomer.All()
        //      .Select(sc => new SalesCustomerViewModel
        //      {
        //            CustomerId = sc.CustomerId,
        //            CustomerName = sc.CustomerName ?? "",
        //            //CustomerCode = sc.CustomerCode,
        //            ShortName = sc.ShortName ?? "",
        //            CustomerAddress = sc.CustomerAddress ==null ? "": sc.CustomerAddress,
        //            //CountryId = sc.CountryId,
        //            //City = sc.City,
        //            //StateOrProvince = sc.StateOrProvince,
        //            //ZipCode = sc.ZipCode,
        //            Phone = sc.Phone ?? "",
        //            Email = sc.Email ?? "",
        //            //Url = sc.Url,
        //            Fax = sc.Fax ?? "",
        //            //Bin = sc.Bin,
        //            //Tin = sc.Tin,
        //            //VatRegNo = sc.VatRegNo,
        //            //ContactPerson = sc.ContactPerson,
        //            OpeningBalance = sc.OpeningBalance ?? 0.00m,
        //            OpeningDate = sc.OpeningDate,
        //            //CreditLimit = sc.CreditLimit,
        //            //Category = sc.Category,
        //            //CustomerType = sc.CustomerType,
        //            //SalesPerson = sc.SalesPersonId,
        //            //OpeningDate = sc.OpeningDate,

        //            // Manually fetching delivery locations based on CustomerId
        //            //DeliveryLocations = salesDeliveryLocation.All()
        //            //    .Where(dl => dl.CustomerId == sc.CustomerId)
        //            //    .Select(dl => new SalesDeliveryLocationViewModel
        //            //    {
        //            //        CustomerId = dl.CustomerId!,
        //            //        DeliveryLocationCode = dl.DeliveryLocationCode,
        //            //        LocationAddress = dl.LocationAddress,
        //            //        ContactPerson = dl.ContactPerson,
        //            //        Phone = dl.Phone,
        //            //        Email = dl.Email,
        //            //        CountryId = dl.CountryId,
        //            //        City = dl.City,
        //            //        StateOrProvince = dl.StateOrProvince,
        //            //        ZipCode = dl.ZipCode,
        //            //        Remarks = dl.Remarks
        //            //    }).ToList()
        //        }).ToListAsync();

        //    return data;
        //}

        public async Task<List<SalesCustomerViewModel>> GetAllAsync()
        {

            try
            {
                var customers = await _salesCustomer.All().ToListAsync();

                // Get delivery locations grouped by CustomerId
                var deliveryLocations = await _salesDeliveryLocation.All()
                    .ToListAsync();

                // Optional: Get all contact persons if needed
                var allContactPersons = await _contactPerson.All().ToListAsync();
                bool isCPNull = allContactPersons == null;
                var result = customers.Select(customer =>
                {

                    var resolvedContacts = new List<SalesContactPersonViewModel>();
                    if (isCPNull)
                    {
                        var customerContactIds = string.IsNullOrWhiteSpace(customer.ContactPerson)
                        ? new List<string>()
                        : customer.ContactPerson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(cp => cp.Trim())
                            .ToList();

                        resolvedContacts = customerContactIds != null
                            ? allContactPersons
                                .Where(cp => customerContactIds.Contains(cp.Cpid))
                                .Select(cp => new SalesContactPersonViewModel
                                {
                                    Cpid = cp.Cpid,
                                    ContactPersonName = cp.ContactPersonName,
                                    ContactPersonEmail = cp.ContactPersonEmail,
                                    ContactPersonMobile = cp.ContactPersonMobile
                                }).ToList()
                            : new List<SalesContactPersonViewModel>();

                    }
                    else
                    {
                        resolvedContacts = null;
                    }

                    var customerDeliveryLocations = deliveryLocations
                        .Where(dl => dl.CustomerId == customer.CustomerId)
                        .Select(dl => new SalesDeliveryLocationViewModel
                        {
                            CustomerId = dl.CustomerId,
                            DeliveryLocationCode = dl.DeliveryLocationCode,
                            LocationAddress = dl.LocationAddress,
                            ContactPerson = dl.ContactPerson,
                            Phone = dl.Phone,
                            Email = dl.Email,
                            CountryId = dl.CountryId,
                            City = dl.City,
                            StateOrProvince = dl.StateOrProvince,
                            ZipCode = dl.ZipCode,
                            Remarks = dl.Remarks
                        }).ToList();

                    return new SalesCustomerViewModel
                    {
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.CustomerName ?? "",
                        ShortName = customer.ShortName ?? "",
                        CustomerType = customer.CustomerType,
                        CustomerAddress = customer.CustomerAddress ?? "",
                        Phone = customer.Phone ?? "",
                        Email = customer.Email ?? "",
                        Fax = customer.Fax ?? "",
                        OpeningBalance = customer.OpeningBalance ?? 0.00m,
                        OpeningDate = customer.OpeningDate,
                        DeliveryLocations = customerDeliveryLocations,
                        ContactPersons = resolvedContacts
                    };
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }


        public async Task<string> GenerateCustomerIdAsync()
        {
            var lastCustomer = await _salesCustomer.All()
            .OrderByDescending(c => c.CustomerId)
            .FirstOrDefaultAsync();

            // Handle the case where no customers exist
            string newCustomerId = lastCustomer.CustomerId;


            return newCustomerId;
        }

        public async Task<bool> BulkDeleteAsync(List<string> customerIds)
        {
            await _salesCustomer.BeginTransactionAsync();

            try
            {
                // Step 1: Get all matching customers
                var customers = await _salesCustomer.All()
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToListAsync();

                if (customers == null || customers.Count == 0)
                {
                    await _salesCustomer.RollbackTransactionAsync();
                    return (false);
                }

                var relatedLocations = await _salesDeliveryLocation.All()
                    .Where(dl => customerIds.Contains(dl.CustomerId))
                    .ToListAsync();

                if (relatedLocations.Any())
                {
                    await _salesDeliveryLocation.DeleteRangeAsync(relatedLocations);
                }

                await _salesCustomer.DeleteRangeAsync(customers);

                await _salesCustomer.CommitTransactionAsync();

                return (true);
            }
            catch (Exception ex)
            {
                await _salesCustomer.RollbackTransactionAsync();
                Console.WriteLine($"Bulk delete error: {ex}");
                return (false);
            }
        }

        #region Customer Report

        public async Task<List<CustomerReportVM>> GetCustomerReportAsync()
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("connection"));
                await connection.OpenAsync();

                var result = await connection.QueryAsync<CustomerReportVM>(
                    "sp_GetCustomerReportsInfo",
                    commandType: System.Data.CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return new List<CustomerReportVM>();
            }
        }


        //public async Task<List<CustomerDeliveryReportVM>> GetCustomerDeliveryReportAsync()
        //{
        //    try
        //    {
        //        using var connection = new SqlConnection(_configuration.GetConnectionString("connection"));
        //        await connection.OpenAsync();

        //        var result = await connection.QueryAsync<CustomerDeliveryReportVM>(
        //            "sp_GetAllCustomerDeliveryInfo",
        //            commandType: System.Data.CommandType.StoredProcedure
        //        );

        //        return result.ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);

        //        return new List<CustomerDeliveryReportVM>();
        //    }
        //}
        public async Task<List<CustomerDeliveryReportVM>> GetCustomerDeliveryReportAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("connection"));
            await connection.OpenAsync();

            var flatList = await connection.QueryAsync<dynamic>(
                "sp_GetAllCustomerDeliveryInfo",
                commandType: CommandType.StoredProcedure
            );

            var customers = flatList
                .GroupBy(c => c.CustomerID)
                .Select(g =>
                {
                    var customer = new CustomerDeliveryReportVM
                    {
                        CustomerID = g.Key,
                        CustomerName = g.First().CustomerName,
                        CustomerAddress = g.First().CustomerAddress,
                        CustomerPhone = g.First().CustomerPhone,
                        CustomerEmail = g.First().CustomerEmail,
                        CustomerType = g.First().CustomerType,
                        CompanyName = g.First().CompanyName,
                        CompanyAddress = g.First().CompanyAddress,
                        FAX = g.First().FAX ?? "",
                        URL = g.First().URL ?? "",
                        BIN = g.First().BIN ?? "",
                        DeliveryDetails = g
                            .Where(d => d.DeliveryLocationCode != null)
                            .Select(d => new DeliveryDetailVM
                            {
                                DeliveryLocationCode = d.DeliveryLocationCode,
                                DeliveryAddress = d.DeliveryAddress,
                                DeliveryPhone = d.DeliveryPhone,
                                DeliveryEmail = d.DeliveryEmail,
                                //ContactPersons = string.IsNullOrEmpty(d.ContactPersonsJson)
                                //    ? new List<ContactPersonVM>()
                                //    : JsonConvert.DeserializeObject<List<ContactPersonVM>>(d.ContactPersonsJson)
                            }).ToList()
                    };
                    return customer;
                }).ToList();

            return customers;
        }

        Task<List<CoreCompany>> ISalesCustomerService.GetCountryDropdownAsync()
        {
            throw new NotImplementedException();
        }



        #endregion
    }
}
