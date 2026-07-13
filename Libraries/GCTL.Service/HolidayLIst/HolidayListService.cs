using GCTL.Core.ViewModels.HolidayLIst;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GCTL.Service.HolidayLIst
{
    public class HolidayListService : IholidayListService
    {
        private readonly IConfiguration _configuration;

        public HolidayListService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<HolidayDto>> GetHolidayListAsync(int year, int? month = null)
        {
            try
            {
                var result = new List<HolidayDto>();
                var connectionString = _configuration.GetConnectionString("connection");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("usp_GetHolidayList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year });
                        cmd.Parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = (object)month ?? DBNull.Value });

                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new HolidayDto
                                {
                                    Id = reader["HolidayID"] != DBNull.Value ? Convert.ToInt32(reader["HolidayID"]) : 0,
                                    HolidayName = reader["HolidayTitle"] != DBNull.Value ? reader["HolidayTitle"].ToString() : string.Empty,
                                    HolidayDescription = reader["HolidayDescription"] != DBNull.Value ? reader["HolidayDescription"].ToString() : string.Empty,
                                    HolidayDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]) : DateTime.MinValue,
                                    EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]) : (DateTime?)null,
                                    TotalDays = reader["TotalDays"] != DBNull.Value ? Convert.ToInt32(reader["TotalDays"]) : 0,
                                    IsActive = reader["StatusID"] != DBNull.Value && Convert.ToInt32(reader["StatusID"]) == 1
                                });
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}