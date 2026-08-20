using GCTL.Core.ViewModels.VoucherEntry;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace GCTL.Service.VoucherEntry
{
    public class VoucherReportService
    {
        #region Service
        public readonly string _connectionString;
        public VoucherReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection");
        }

        #endregion


        #region Calling Store Procedure
        public async Task<List<VoucherPreviewVM>> GetVoucherPreviewDataAsync(List<decimal> ids)
        {
            //SqlConnection
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_GetVoucherPreviewData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    //Parameter adding
                    command.Parameters.AddWithValue("@Ids", string.Join(",", ids));

                    var result = new List<VoucherPreviewVM>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new VoucherPreviewVM
                            {
                                CompanyName = reader["CompanyName"]?.ToString(),
                                CompanyAddress = reader["CompanyAddress"]?.ToString(),
                                VoucherNo = reader["VoucherNo"]?.ToString(),
                                VoucherDate = Convert.ToDateTime(reader["VoucherDate"]),
                                Narration = reader["Narration"]?.ToString(),
                                VoucherTypeName = reader["VoucherTypeName"].ToString(),
                                AccCode = reader["AccCode"]?.ToString(),
                                SubSubsidiaryLedgerName = reader["SubSubsidiaryLedgerName"]?.ToString(),
                                DebitAmount = reader["DebitAmount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["DebitAmount"])
                                    : (decimal?)null,
                                CreditAmount = reader["CreditAmount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["CreditAmount"])
                                    : (decimal?)null,
                                PreparedBy = reader["PreparedBy"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }

                    return result;
                }
            }
        }

        #endregion
    }
}
