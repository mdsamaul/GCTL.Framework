using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.SepDocumentation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.SepDocumentation
{
    public class SepDocumentationService : ISepDocumentationService
    {
        private readonly IGenericRepository<GCTL.Data.Models.SalesCustomer> _customerRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SepDocumentation> _sepDocumentationRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SalesDeliveryLocation> _deliveryLocationRepository;
        private readonly IConfiguration _configuration;

      

        public SepDocumentationService(IGenericRepository<GCTL.Data.Models.SalesCustomer> customerRepository, IGenericRepository<GCTL.Data.Models.SalesDeliveryLocation> deliveryLocationRepository, IGenericRepository<GCTL.Data.Models.SepDocumentation> sepDocumentationRepository, IConfiguration configuration)
        {
            

            _customerRepository = customerRepository;
            _deliveryLocationRepository = deliveryLocationRepository;
            _sepDocumentationRepository = sepDocumentationRepository;
            _configuration = configuration;
        }

        public async Task<CommonReturnViewModel> AddDocumentation(GCTL.Data.Models.SepDocumentation model)
        {
            try
            {
                var exists = await _sepDocumentationRepository.All()
                .AnyAsync(x => x.InvoiceNo == model.InvoiceNo || x.ExpNo == model.ExpNo);

                if (exists)
                    return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };

                model.Ldate = DateTime.Now;
                model.ModifyDate = DateTime.Now;

                await _sepDocumentationRepository.AddAsync(model);


                return new CommonReturnViewModel
                {
                    Success = true,
                    Message =  "Data added successfully" 
                };
            }
            catch (Exception)
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message =  "Failed to add data"
                };
            }
            
        }

        public async Task<CommonReturnViewModel> UpdateDocumentation(GCTL.Data.Models.SepDocumentation model)
        {
            try
            {
                var existing = await _sepDocumentationRepository.All()
                .FirstOrDefaultAsync(x => x.Tc == model.Tc);

                if (existing == null)
                    return new CommonReturnViewModel { Success = false, Message = "Job not found" };

                // Check duplicate ExpNo/InvoiceNo (excluding current record)
                var duplicate = await _sepDocumentationRepository.All()
                    .AnyAsync(x => x.Tc != model.Tc &&
                                  (x.InvoiceNo == model.InvoiceNo || x.ExpNo == model.ExpNo));

                if (duplicate)
                    return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };

                // Update fields
                existing.Date = model.Date;
                existing.CustomerId = model.CustomerId;
                existing.CustomerAddress = model.CustomerAddress;
                existing.DocReceivedDate = model.DocReceivedDate;
                existing.ExpNo = model.ExpNo;
                existing.ExpDate = model.ExpDate;
                existing.Lcno = model.Lcno;
                existing.Lcdate = model.Lcdate;
                existing.Lcvalue = model.Lcvalue;
                existing.InvoiceNo = model.InvoiceNo;
                existing.InvoiceDate = model.InvoiceDate;
                existing.InvoiceValue = model.InvoiceValue;
                 //existing.BLNo = model.BLNo;
                existing.Bldate = model.Bldate;
                existing.Beno = model.Beno;
                existing.Bedate = model.Bedate;
                existing.ContainerNo = model.ContainerNo;
                existing.ContainerSize = model.ContainerSize;
                existing.MaterialDescription = model.MaterialDescription;
                existing.Quntity1 = model.Quntity1;
                existing.Unit1 = model.Unit1;
                existing.Quntity2 = model.Quntity2;
                existing.Unit2 = model.Unit2;
                existing.FwdrShippingAgent = model.FwdrShippingAgent;
                existing.Dischargedate = model.Dischargedate;
                existing.Eta = model.Eta;
                existing.Etactgport = model.Etactgport;
                existing.UnStuffingLocationDate = model.UnStuffingLocationDate;
                existing.Etddate = model.Etddate;
                existing.VesselRottNo = model.VesselRottNo;
                existing.DateOfClearence = model.DateOfClearence;
                existing.Ata = model.Ata;
                existing.PlaceOfLoadingId = model.PlaceOfLoadingId;
                existing.ShedYardId = model.ShedYardId;
                existing.Hawb = model.Hawb;
                existing.Mawb = model.Mawb;
                existing.NameOfFreightForwarder = model.NameOfFreightForwarder;
                existing.AssessmentNo = model.AssessmentNo;
                existing.Rno = model.Rno;
                existing.ClientStationwiseSlno = model.ClientStationwiseSlno;
                existing.FreightCharge = model.FreightCharge;
                existing.ShipmentStatus = model.ShipmentStatus;
                existing.Remarks = model.Remarks;
                existing.PackingQuantity = model.PackingQuantity;
                existing.PackingUnitType = model.PackingUnitType;
                existing.AssessableRate = model.AssessableRate;
                existing.AssessableValue = model.AssessableValue;
                existing.Bdtrate = model.Bdtrate;
                existing.DeliveryLocationCode = model.DeliveryLocationCode;


                existing.ModifyDate = DateTime.Now;

                await _sepDocumentationRepository.UpdateAsync(existing);


                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Record updated successfully"
                };
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        public async Task<CommonReturnViewModel> DeleteDocumentation(string jobNo)
        {
            var entity = await _sepDocumentationRepository.All()
                .FirstOrDefaultAsync(x => x.JobNo == jobNo);

            if (entity == null)
                return new CommonReturnViewModel { Success = false, Message = "Job not found" };

            await _sepDocumentationRepository.DeleteAsync(entity);
            

            return new CommonReturnViewModel
            {
                Success = true,
                Message =  "Record deleted successfully" 
            };
        }

        public GCTL.Data.Models.SepDocumentation GetDocumentationByTC(decimal tc)
        {
            var data  = _sepDocumentationRepository.All().FirstOrDefault(e=>e.Tc == tc);
            return data ?? new GCTL.Data.Models.SepDocumentation();
        }

        #region Get Data

        private async Task<bool> CheckAndCreateStoredProcedureAsync(SqlConnection connection)
        {
            try
            {
                // Check if stored procedure exists
                var checkQuery = @"
                        SELECT COUNT(*) 
                        FROM sys.procedures 
                        WHERE name = 'SP_GetDocumentationList'";

                var exists = await connection.ExecuteScalarAsync<int>(checkQuery);

                if (exists > 0)
                {
                    Console.WriteLine("Stored procedure already exists.");
                    return true;
                }

                Console.WriteLine("Stored procedure not found. Creating...");

                // Create the stored procedure
                var createProcQuery = @"
                        CREATE or alter PROCEDURE [dbo].[SP_GetDocumentationList]
                            @Page INT = 1,
                            @PageSize INT = 10,
                            @Search NVARCHAR(255) = NULL,
                            @SortColumn NVARCHAR(50) = 'JobNo',
                            @SortDirection NVARCHAR(4) = 'asc',
                            @DateFrom DATE = NULL,
                            @DateTo DATE = NULL,
                            @CustomerID NVARCHAR(50) = NULL
                        AS
                        BEGIN
                            SET NOCOUNT ON;

                            DECLARE @Offset INT = (@Page - 1) * @PageSize;
                            DECLARE @TotalRecords INT;

                            -- Build dynamic query
                            DECLARE @SQL NVARCHAR(MAX);
                            DECLARE @CountSQL NVARCHAR(MAX);
                            DECLARE @WhereClause NVARCHAR(MAX) = ' WHERE 1=1 ';
    
                            -- Apply filters
                            IF @Search IS NOT NULL AND @Search != ''
                            BEGIN
                                SET @WhereClause = @WhereClause + 
                                    ' AND (doc.JobNo LIKE ''%' + @Search + '%'' 
                                    OR cus.CustomerName LIKE ''%' + @Search + '%''
                                    OR doc.CustomerAddress11 LIKE ''%' + @Search + '%'')';
                            END

                            IF @DateFrom IS NOT NULL
                            BEGIN
                                SET @WhereClause = @WhereClause + ' AND doc.Date >= ''' + CONVERT(VARCHAR(10), @DateFrom, 23) + '''';
                            END

                            IF @DateTo IS NOT NULL
                            BEGIN
                                SET @WhereClause = @WhereClause + ' AND doc.Date <= ''' + CONVERT(VARCHAR(10), @DateTo, 23) + '''';
                            END

                            IF @CustomerID IS NOT NULL AND @CustomerID != ''
                            BEGIN
                                SET @WhereClause = @WhereClause + ' AND doc.CustomerID = ''' + @CustomerID + '''';
                            END

                            -- Get total count
                            SET @CountSQL = '
                            SELECT @TotalRecords = COUNT(*)
                            FROM SepDocumentation doc
                            LEFT JOIN CF_Def_ExpenseType ship ON doc.ExpenseTypeID = ship.ExpenseTypeID
                            LEFT JOIN SalesCustomer cus ON doc.CustomerID = cus.CustomerID' + @WhereClause;

                            EXEC sp_executesql @CountSQL, N'@TotalRecords INT OUTPUT', @TotalRecords OUTPUT;

                            -- Build order by clause
                            DECLARE @OrderBy NVARCHAR(100);
                            SET @OrderBy = CASE @SortColumn
                                WHEN 'TC' THEN 'doc.tc'
                                WHEN 'JobNo' THEN 'doc.JobNo'
                                WHEN 'Date' THEN 'doc.Date'
                                WHEN 'CustomerName' THEN 'cus.CustomerName'
                                WHEN 'CustomerAddress11' THEN 'doc.CustomerAddress11'
                                WHEN 'ShipmentMode' THEN 'ship.ShortName'
                                WHEN 'DocReceivedDate' THEN 'doc.DocReceivedDate'
                                WHEN 'LCValue' THEN 'doc.LCValue'
                                WHEN 'Status' THEN 'doc.Status'
                                ELSE 'doc.JobNo'
                            END + ' ' + @SortDirection;

                            -- Get paginated data
                            SET @SQL = '
                            SELECT 
                                doc.tc,
                                doc.JobNo,
                                doc.Date,
                                cus.CustomerName,
                                doc.CustomerAddress11,
                                ship.ShortName AS ShipmentMode,
                                doc.DocReceivedDate,
                                doc.LCValue,
       
                                ' + CAST(@TotalRecords AS NVARCHAR(10)) + ' AS TotalRecords
                            FROM SepDocumentation doc
                            LEFT JOIN CF_Def_ExpenseType ship ON doc.ExpenseTypeID = ship.ExpenseTypeID
                            LEFT JOIN SalesCustomer cus ON doc.CustomerID = cus.CustomerID' 
                            + @WhereClause + '
                            ORDER BY ' + @OrderBy + '
                            OFFSET ' + CAST(@Offset AS NVARCHAR(10)) + ' ROWS
                            FETCH NEXT ' + CAST(@PageSize AS NVARCHAR(10)) + ' ROWS ONLY';

                            EXEC sp_executesql @SQL;
	
	
	                        --EXEC SP_GetDocumentationList     @Page = 1,    @PageSize = 10,    
	                        --								@Search = NULL,    @SortColumn = 'JobNo',    
	                        --								@SortDirection = 'asc',    @DateFrom = NULL,    
	                        --								@DateTo = NULL,    @CustomerID = 'CUS000004'


                        END



                        ";

                await connection.ExecuteAsync(createProcQuery);
                Console.WriteLine("Stored procedure created successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking/creating stored procedure: {ex.Message}");
                return false;
            }
        }


        public async Task<(List<DocumentationResult> Data, int TotalRecords)> GetDocumentationList(int page, int pageSize, string search, string sortColumn, string sortDirection, DateTime? dateFrom, DateTime? dateTo, string customerId)
        {
            try
            {
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@Page", page);
                queryParameters.Add("@PageSize", pageSize);
                queryParameters.Add("@Search", string.IsNullOrWhiteSpace(search) ? null : search.Trim());
                queryParameters.Add("@SortColumn", sortColumn);
                queryParameters.Add("@SortDirection", sortDirection);
                queryParameters.Add("@DateFrom", dateFrom);
                queryParameters.Add("@DateTo", dateTo);
                queryParameters.Add("@CustomerID", string.IsNullOrWhiteSpace(customerId) ? null : customerId);

                var query = BuildQueryString(queryParameters);
                Console.WriteLine($"Generated Query: {query}");

                using (var connection = new SqlConnection(_configuration.GetConnectionString("connection")))
                {
                    await connection.OpenAsync();

                    await CheckAndCreateStoredProcedureAsync(connection);

                    var result = (await connection.QueryAsync<DocumentationResult>(
                        query,
                        queryParameters
                    )).ToList();

                    var data = result.Select(item => new DocumentationResult
                    {
                        tc = item.tc,
                        JobNo = item.JobNo,
                        Date = item.Date,
                        CustomerName = item.CustomerName,
                        CustomerAddress = item.CustomerAddress,
                        ShipmentMode = item.ShipmentMode,
                        DocReceivedDate = item.DocReceivedDate,
                        LCValue = item.LCValue,
                        Status = item.Status
                    }).ToList();

                    var totalRecords = result.FirstOrDefault()?.TotalRecords ?? 0;

                    return (data, totalRecords);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching documentation list: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }




        private string BuildQueryString(DynamicParameters parameters)
        {
            var queryBuilder = new StringBuilder();
            queryBuilder.Append("EXEC SP_GetDocumentationList ");
            bool hasParameters = false;

            foreach (var param in parameters.ParameterNames)
            {
                var value = parameters.Get<object>(param);

                // Skip null parameters
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    continue;

                // Format based on type
                string formattedValue;
                if (value is string)
                    formattedValue = $"'{value}'";
                else if (value is DateTime dt)
                    formattedValue = $"'{dt:yyyy-MM-dd}'";
                else
                    formattedValue = value.ToString();

                queryBuilder.Append($"@{param} = {formattedValue}, ");
                hasParameters = true;
            }

            if (hasParameters)
                queryBuilder.Length -= 2; // Remove trailing comma and space

            return queryBuilder.ToString();
        }

        #endregion

        public List<SelectListItem> GetCustomers()
        {
            var customers = _customerRepository.All().ToList();
            return customers.Select(c => new SelectListItem
            {
                Text = c.CustomerName,
                Value = c.CustomerId
            }).ToList();
        }

        public List<SelectListItem> GetAddresses()
        {
            var addresses = _deliveryLocationRepository.All().ToList();
            return addresses.Select(a => new SelectListItem 
            {
                Text = a.LocationAddress,
                Value = a.DeliveryLocationCode
            }).ToList();
        }
    }
    public class SelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }


    public class DocumentationResult
    {
        public string tc { get; set; }
        public string JobNo { get; set; }
        public DateTime? Date { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string ShipmentMode { get; set; }
        public DateTime? DocReceivedDate { get; set; }
        public decimal? LCValue { get; set; }
        public string Status { get; set; }
        public int TotalRecords { get; set; }
    }


}
