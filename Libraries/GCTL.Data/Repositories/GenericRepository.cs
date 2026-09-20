using GCTL.Core.Data;
using GCTL.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GCTL.Data.Repositories;

/// <summary>
/// Generic repository, restored from your original GCTL.Data.GenericRepository.
/// Retargeted from the old GCTL_ERP_DB_DatapathContext to the new
/// GctlHrmContext (the placeholder that EF Power Tools will fill in once you
/// reverse-engineer your real database). Everything else works identically
/// to your original implementation.
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly GctlHrmContext _context;

    public GenericRepository(GctlHrmContext context)
    {
        _context = context;
    }

    public IQueryable<T> All() => _context.Set<T>();

    public IEnumerable<T> GetAll() => _context.Set<T>().ToList();

    public IQueryable<T> FindBy(Expression<Func<T, bool>> expression) => _context.Set<T>().Where(expression);

    public IQueryable<T> Get(FormattableString sql) =>
        _context.Set<T>().FromSqlInterpolated(sql);

    public T? GetById(object id) => _context.Set<T>().Find(id);

    public T Add(T entity)
    {
        _context.Set<T>().Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public void Add(IEnumerable<T> entities)
    {
        _context.Set<T>().AddRange(entities);
        _context.SaveChanges();
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
        _context.SaveChanges();
    }

    public void Update(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        _context.SaveChanges();
    }

    public void Delete(object id)
    {
        var entity = GetById(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            _context.SaveChanges();
        }
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
        _context.SaveChanges();
    }

    public void Delete(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        _context.SaveChanges();
    }

    // async
    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<IQueryable<T>> AllAsync() => await Task.FromResult(_context.Set<T>());

    public async Task<IQueryable<T>> FindByAsync(Expression<Func<T, bool>> expression) =>
        await Task.FromResult(_context.Set<T>().Where(expression));

    public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();

    public async Task<T?> GetByIdAsync(object id) => await _context.Set<T>().FindAsync(id);

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(object id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.CommitAsync();
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
        }
    }

    public async Task DisposeTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.DisposeAsync();
        }
    }

    public string GetTableName()
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        return entityType?.GetTableName() ?? typeof(T).Name;
    }

    #region Dependency Check and Delete with DeleteHistory (Field1..99)

    public async Task<GCTL.Core.ViewModels.DeleteHistories.DependencyCheckResult> CheckDependenciesAsync(
        string keyColumnName,
        List<string> keyValues,
        List<string>? alternateColumns = null,
        List<string>? ignoreTables = null)
    {
        if (string.IsNullOrWhiteSpace(keyColumnName)) throw new ArgumentNullException(nameof(keyColumnName));
        if (keyValues == null || keyValues.Count == 0) throw new ArgumentNullException(nameof(keyValues));

        var masterTableName = GetTableName();
        var ignoreSet = new HashSet<string>(
            ignoreTables?.Where(t => !string.IsNullOrWhiteSpace(t)) ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase
        );

        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { keyColumnName };
        if (alternateColumns != null)
        {
            foreach (var a in alternateColumns.Where(x => !string.IsNullOrWhiteSpace(x)))
                columnNames.Add(a);
        }

        var result = new GCTL.Core.ViewModels.DeleteHistories.DependencyCheckResult { CanDelete = true, Message = string.Empty };
        var candidates = await DiscoverCandidateColumnsAsync(columnNames, masterTableName);

        if (candidates == null || candidates.Count == 0)
        {
            result.CanDelete = true;
            result.Message = "No candidate reference columns found.";
            return result;
        }

        var filteredCandidates = candidates
            .Where(c => !string.Equals(c.TableName, masterTableName, StringComparison.OrdinalIgnoreCase))
            .Where(c => !IsTableIgnored(c.TableSchema, c.TableName, ignoreSet))
            .ToList();

        if (filteredCandidates.Count == 0)
        {
            result.Message = "No references found. Safe to delete.";
            return result;
        }

        var stringValues = keyValues.Select(v => v?.ToString() ?? string.Empty).ToList();
        var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        var unionParts = new List<string>();
        var allParams = new Dictionary<string, object>();
        int totalParams = 0;
        const int SQL_SERVER_PARAM_LIMIT = 2000;

        for (int tableIndex = 0; tableIndex < filteredCandidates.Count; tableIndex++)
        {
            var cand = filteredCandidates[tableIndex];
            var paramNames = new List<string>();

            for (int i = 0; i < stringValues.Count; i++)
            {
                var pName = $"@t{tableIndex}_p{i}";
                allParams[pName] = stringValues[i] ?? (object)DBNull.Value;
                paramNames.Add(pName);
                totalParams++;
            }

            if (totalParams > SQL_SERVER_PARAM_LIMIT)
            {
                filteredCandidates = filteredCandidates.Take(tableIndex).ToList();
                break;
            }

            string schemaQ = QuoteIdentifier(cand.TableSchema);
            string tableQ = QuoteIdentifier(cand.TableName);
            string columnQ = QuoteIdentifier(cand.ColumnName);
            string inClause = string.Join(", ", paramNames);

            unionParts.Add($@"SELECT {tableIndex} AS TableIdx, COUNT(1) AS Cnt
                              FROM {schemaQ}.{tableQ}
                              CROSS APPLY dbo.STRING_SPLIT(',', {columnQ}) AS split
                              WHERE LTRIM(RTRIM(split.Value)) IN ({inClause})");
        }

        var countByIndex = new Dictionary<int, int>();
        var dependentLines = new List<string>();

        if (unionParts.Count > 0)
        {
            var fullSql = string.Join("\nUNION ALL\n", unionParts);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = fullSql;
            cmd.CommandType = System.Data.CommandType.Text;

            foreach (var kv in allParams)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = kv.Key;
                p.Value = kv.Value;
                p.DbType = System.Data.DbType.String;
                cmd.Parameters.Add(p);
            }

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int idx = reader.GetInt32(0);
                    int cnt = reader.GetInt32(1);
                    countByIndex[idx] = cnt;
                }
            }
            catch (Exception)
            {
                // Fallback direct check if union fails
                foreach (var cand in filteredCandidates)
                {
                    var count = await CheckSingleCandidateAsync(conn, cand, stringValues);
                    if (count > 0)
                    {
                        result.CanDelete = false;
                        var friendlyName = await GetFriendlyNameForTableAsync(conn, cand.TableSchema, cand.TableName);
                        dependentLines.Add(friendlyName);
                    }
                }
            }
        }

        for (int i = 0; i < filteredCandidates.Count; i++)
        {
            var cand = filteredCandidates[i];
            var count = countByIndex.GetValueOrDefault(i, 0);

            if (count > 0)
            {
                result.CanDelete = false;
                var friendlyName = await GetFriendlyNameForTableAsync(conn, cand.TableSchema, cand.TableName);
                dependentLines.Add(friendlyName);
            }
        }

        result.DependentTables = dependentLines.Distinct().ToList();
        BuildResultMessage(result);
        return result;
    }

    public async Task<(bool Success, string Message)> DeleteWithAuditAsync(
        string keyColumnName,
        List<string> keyValues,
        List<string>? alternateColumns = null,
        List<string>? ignoreTables = null,
        GCTL.Core.ViewModels.DeleteHistories.DeleteHistoryViewModel? auditInfo = null)
    {
        if (keyValues == null || keyValues.Count == 0)
            return (false, "No identifiers provided for deletion.");

        // 1. Check referential integrity across loosely-coupled tables
        var dependencyCheck = await CheckDependenciesAsync(keyColumnName, keyValues, alternateColumns, ignoreTables);
        if (!dependencyCheck.CanDelete)
            return (false, dependencyCheck.Message);

        var tableName = GetTableName();

        // 2. Fetch entities to be deleted
        var property = typeof(T).GetProperty(keyColumnName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        List<T> entitiesToDelete;

        if (property != null)
        {
            var allEntities = await _context.Set<T>().ToListAsync();
            entitiesToDelete = allEntities.Where(e =>
            {
                var val = property.GetValue(e)?.ToString();
                return val != null && keyValues.Contains(val, StringComparer.OrdinalIgnoreCase);
            }).ToList();
        }
        else
        {
            return (false, $"Key column '{keyColumnName}' not found on entity type '{typeof(T).Name}'.");
        }

        if (entitiesToDelete.Count == 0)
            return (false, "No matching records found to delete.");

        // 3. Begin transaction & perform delete + DeleteHistory logging
        await BeginTransactionAsync();
        try
        {
            _context.Set<T>().RemoveRange(entitiesToDelete);

            // Log deleted records to DeleteHistory (Field1..Field99 + JSON)
            await LogDeletedRecordsInternalAsync(entitiesToDelete, tableName, auditInfo);

            await _context.SaveChangesAsync();
            await CommitTransactionAsync();
            return (true, "Deleted Successfully.");
        }
        catch (Exception ex)
        {
            await RollbackTransactionAsync();
            return (false, $"Delete Failed: {ex.Message}");
        }
    }

    private async Task LogDeletedRecordsInternalAsync(List<T> entities, string tableName, GCTL.Core.ViewModels.DeleteHistories.DeleteHistoryViewModel? auditInfo)
    {
        if (entities == null || entities.Count == 0) return;

        var deleteHistorySet = _context.Set<GCTL.Data.Models.DeleteHistory>();
        decimal maxDhid = 0;
        try
        {
            maxDhid = await deleteHistorySet.MaxAsync(dh => (decimal?)dh.Dhid) ?? 0;
        }
        catch
        {
            maxDhid = 0;
        }

        decimal currentDhid = maxDhid + 1;
        var deleteHistoryRecords = new List<GCTL.Data.Models.DeleteHistory>();
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var entity in entities)
        {
            var dh = new GCTL.Data.Models.DeleteHistory
            {
                Dhid = currentDhid++,
                TableName = tableName,
                Lip = auditInfo?.Lip ?? string.Empty,
                Lmac = auditInfo?.Lmac ?? string.Empty,
                Ldate = auditInfo?.Ldate ?? DateTime.Now,
                Luser = auditInfo?.Luser ?? string.Empty,
                CompanyCode = auditInfo?.CompanyCode ?? string.Empty,
                UserInfoEmployeeId = auditInfo?.UserInfoEmployeeId ?? string.Empty
            };

            int fieldIndex;
            for (fieldIndex = 0; fieldIndex < Math.Min(properties.Length, 99); fieldIndex++)
            {
                var prop = properties[fieldIndex];
                var val = prop.GetValue(entity)?.ToString();
                var fieldProp = typeof(GCTL.Data.Models.DeleteHistory).GetProperty($"Field{fieldIndex + 1}");
                fieldProp?.SetValue(dh, val);
            }

            // Save JSON snapshot in Field{N+1}
            var jsonProp = typeof(GCTL.Data.Models.DeleteHistory).GetProperty($"Field{fieldIndex + 1}");
            if (jsonProp != null)
            {
                var dict = new Dictionary<string, object?> { { tableName, entity } };
                jsonProp.SetValue(dh, System.Text.Json.JsonSerializer.Serialize(dict));
            }

            deleteHistoryRecords.Add(dh);
        }

        await deleteHistorySet.AddRangeAsync(deleteHistoryRecords);
    }

    private static string QuoteIdentifier(string ident) => string.IsNullOrEmpty(ident) ? ident : "[" + ident.Replace("]", "]]") + "]";

    private async Task<List<CandidateColumn>> DiscoverCandidateColumnsAsync(HashSet<string> columnNames, string masterTableName)
    {
        var list = new List<CandidateColumn>();
        if (columnNames == null || columnNames.Count == 0) return list;

        var conn = _context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.Text;

        var paramNames = new List<string>();
        int idx = 0;
        foreach (var c in columnNames)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@cn{idx}";
            p.Value = c;
            p.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(p);
            paramNames.Add(p.ParameterName);
            idx++;
        }

        cmd.CommandText = $@"
            SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE COLUMN_NAME IN ({string.Join(", ", paramNames)})
            AND (DATA_TYPE = 'nvarchar' OR DATA_TYPE = 'varchar' OR DATA_TYPE = 'char' OR DATA_TYPE = 'nchar')
            AND TABLE_NAME <> @masterTableName
            AND TABLE_SCHEMA NOT IN ('INFORMATION_SCHEMA', 'sys')
        ";

        var masterParam = cmd.CreateParameter();
        masterParam.ParameterName = "@masterTableName";
        masterParam.Value = masterTableName;
        masterParam.DbType = System.Data.DbType.String;
        cmd.Parameters.Add(masterParam);

        using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new CandidateColumn { TableSchema = rdr.GetString(0), TableName = rdr.GetString(1), ColumnName = rdr.GetString(2) });
        }

        return list;
    }

    private async Task<int> CheckSingleCandidateAsync(System.Data.Common.DbConnection conn, CandidateColumn cand, List<string> stringValues)
    {
        using var cmd = conn.CreateCommand();
        var paramNames = new List<string>();
        for (int i = 0; i < stringValues.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@p{i}";
            p.Value = stringValues[i] ?? (object)DBNull.Value;
            p.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(p);
            paramNames.Add(p.ParameterName);
        }

        string schemaQ = QuoteIdentifier(cand.TableSchema);
        string tableQ = QuoteIdentifier(cand.TableName);
        string columnQ = QuoteIdentifier(cand.ColumnName);
        string inClause = string.Join(", ", paramNames);

        cmd.CommandText = $@"
            SELECT COUNT(1)
            FROM {schemaQ}.{tableQ}
            CROSS APPLY dbo.STRING_SPLIT(',', {columnQ}) AS split
            WHERE LTRIM(RTRIM(split.Value)) IN ({inClause})
        ";

        var scalar = await cmd.ExecuteScalarAsync();
        return (scalar != null && scalar != DBNull.Value) ? Convert.ToInt32(scalar) : 0;
    }

    private async Task<string> GetFriendlyNameForTableAsync(System.Data.Common.DbConnection conn, string schema, string tableName)
    {
        var tableNames = tableName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tableNames.Length > 1)
        {
            var names = new List<string>();
            foreach (var singleTable in tableNames)
                names.Add(await GetFriendlyNameForSingleTableAsync(conn, schema, singleTable));
            return string.Join(", ", names);
        }

        return await GetFriendlyNameForSingleTableAsync(conn, schema, tableName.Trim());
    }

    private async Task<string> GetFriendlyNameForSingleTableAsync(System.Data.Common.DbConnection conn, string schema, string tableName)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            var p = cmd.CreateParameter();
            p.ParameterName = "@tbl";
            p.Value = tableName;
            p.DbType = System.Data.DbType.String;
            cmd.Parameters.Add(p);

            cmd.CommandText = @"
                SELECT TOP (1) COALESCE(NULLIF(ac.Title,''), NULLIF(ac.MenuText,''), NULLIF(ac.AccessCodeName,''), NULLIF(m.Title,'')) AS FriendlyName
                FROM dbo.Core_AccessCode ac
                LEFT JOIN dbo.Core_MenuTab2 m ON ac.MenuId = m.MenuId
                WHERE m.TableName = @tbl
            ";

            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar != null && scalar != DBNull.Value && !string.IsNullOrWhiteSpace(scalar.ToString()))
                return scalar.ToString()!;
        }
        catch { }

        var prefixes = new[]
        {
            "RMG_Prod_Def", "RMG_Prod_Temp", "RMG_Pro_BTB", "RMG_Prod", "RMG_Def", "RMG_Inv", "RMG",
            "HRM_Payroll", "HRM_ATD", "HRM_Att", "HRM_Def", "HRM_PAY", "HRM",
            "Def_Inv", "Inv_Def", "INV", "Prod_Def", "Sales_Def", "TB_Def", "CA_Def",
            "Core", "Acc", "POS", "TBM", "tbl", "dbo."
        };

        var pretty = tableName;
        foreach (var prefix in prefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && pretty.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                pretty = pretty.Substring(prefix.Length);
                break;
            }
        }

        pretty = pretty.Replace("_", " ").Trim();
        pretty = System.Text.RegularExpressions.Regex.Replace(pretty, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        if (pretty.EndsWith(" Entry", StringComparison.OrdinalIgnoreCase))
            pretty = pretty[..^6].TrimEnd();

        return string.IsNullOrWhiteSpace(pretty) ? tableName : pretty;
    }

    private bool IsTableIgnored(string tableSchema, string tableName, HashSet<string> ignoreSet)
    {
        string normalizedCandidate = GCTL.Core.Helpers.NormalizedHelpers.ToNormalizedTableKey(tableName);
        foreach (var entry in ignoreSet)
        {
            if (string.Equals(entry, tableName, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(entry, $"{tableSchema}.{tableName}", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(GCTL.Core.Helpers.NormalizedHelpers.ToNormalizedTableKey(entry), normalizedCandidate, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void BuildResultMessage(GCTL.Core.ViewModels.DeleteHistories.DependencyCheckResult result)
    {
        if (!result.CanDelete)
        {
            string joined = string.Join(", ", result.DependentTables);
            if (result.DependentTables.Count > 1)
            {
                int lastComma = joined.LastIndexOf(", ");
                if (lastComma >= 0)
                    joined = joined.Substring(0, lastComma) + " & " + joined.Substring(lastComma + 1);

                result.Message = $"Can't be deleted. Please delete first from {joined} respectively.";
            }
            else
            {
                result.Message = $"Can't be deleted. Please delete first from {joined}.";
            }
        }
        else
        {
            result.Message = "No references found. Safe to delete.";
        }
    }

    private class CandidateColumn
    {
        public string TableSchema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
    }

    #endregion

}

/* ====================================================================================================
 * HOW TO APPLY DeleteWithAuditAsync IN SERVICES AND CONTROLLERS (EXAMPLE USAGE GUIDE)
 * ====================================================================================================
 *
 * 1. IN YOUR SERVICE (e.g. VoucherTypeService.cs or any other service):
 * ----------------------------------------------------------------------------------------------------
 * public async Task<(bool Success, string Message)> BulkDeleteAsync(List<string> ids, DeleteHistoryViewModel? auditInfo = null)
 * {
 *     // Simple 1-liner! Automatically checks foreign dependencies across loosely-coupled DB tables,
 *     // starts transaction, deletes matching rows, logs them to DeleteHistory (Field1..Field99 + JSON),
 *     // commits transaction, and returns a user-friendly error or success message.
 *     return await _repository.DeleteWithAuditAsync(
 *         keyColumnName: nameof(AccVoucherType.VoucherTypeCode),
 *         keyValues: ids,
 *         alternateColumns: new List<string> { "VoucherTypeCode", "VoucherTypeName" },
 *         ignoreTables: null,
 *         auditInfo: auditInfo
 *     );
 * }
 *
 * 2. IN YOUR CONTROLLER (e.g. VoucherTypeController.cs):
 * ----------------------------------------------------------------------------------------------------
 * [HttpDelete]
 * [Route("voucher-type-all-delete")]
 * public async Task<IActionResult> BulkDelete([FromBody] List<string> ids)
 * {
 *     if (ids == null || !ids.Any())
 *         return Json(new { isSuccess = false, message = "No Data Is Selected To Delete" });
 *
 *     var auditInfo = new DeleteHistoryViewModel().ToAudit(LoginInfo);
 *     auditInfo.CompanyCode = LoginInfo.CompanyCode;
 *
 *     var (success, message) = await _service.BulkDeleteAsync(ids, (DeleteHistoryViewModel)auditInfo);
 *     return Json(new { isSuccess = success, message = message });
 * }
 *
 * Note: Existing pages using IDeleteHistoryService directly are 100% unaffected and continue working.
 * ==================================================================================================== */
