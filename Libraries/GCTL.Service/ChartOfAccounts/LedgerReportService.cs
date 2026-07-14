using Dapper;
using GCTL.Core.ViewModels.ChartOfAccount;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace GCTL.Service.ChartOfAccounts
{
    public class LedgerReportService
    {
        public readonly string _connectionString;
        public LedgerReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection");
        }
        public List<TreeLedgerReportVM> GetLedgerTreeDataAsync()
        {
            List<TreeLedgerReportVM> ledgerList = new List<TreeLedgerReportVM>();

            using (var con = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand("GetLedgerTree", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ledgerList.Add(new TreeLedgerReportVM
                            {
                                CodeNo = reader["CodeNo"].ToString(),
                                Name = reader["Name"].ToString(),
                                ParentCodeNo = reader["ParentCodeNo"] == DBNull.Value
                                    ? null
                                    : reader["ParentCodeNo"].ToString()
                            });
                        }
                    }
                }
            }

            // Build hierarchical tree
            var lookup = ledgerList.ToLookup(l => l.ParentCodeNo);
            foreach (var node in ledgerList)
            {
                node.Children = lookup[node.CodeNo].ToList();

                // Set parent name for children
                foreach (var child in node.Children)
                {
                    child.ParentName = node.Name;
                }
            }

            return ledgerList.Where(l => l.ParentCodeNo == null).ToList();
        }

        public string GetCompanyName()
        {
            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT CompanyName FROM Core_Company", con))
            {
                con.Open();
                return cmd.ExecuteScalar()?.ToString() ?? "";
            }
        }
    }
}
