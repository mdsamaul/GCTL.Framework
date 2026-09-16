using GCTL.Core.DataTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace GCTL.Service.Pagination
{
    public static class DeferredPaginationService
    {
        public static async Task<PaginationResponse<TEntity>> GetPaginatedDataAsync<TEntity, TKey>(
            IQueryable<TEntity> query,
            PaginationRequest request,
            Expression<Func<TEntity, TKey>> keySelector) where TEntity : class
        {
            query = query.AsNoTracking();

            // If a search term is entered, route through the dynamic searchable pipeline
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return await PaginationService<TEntity>.GetPaginatedData(query, request);
            }

            var totalItems = await query.CountAsync();
            if (totalItems == 0)
            {
                return new PaginationResponse<TEntity>
                {
                    Data = new List<TEntity>(),
                    TotalCount = 0,
                    FilteredCount = 0,
                    PaginationInfo = new PaginationResponse<TEntity>.PaginationInfo2
                    {
                        StartItem = 0,
                        EndItem = 0,
                        TotalItems = 0,
                        TotalPages = 0,
                        CurrentPage = request.PageNumber <= 0 ? 1 : request.PageNumber
                    }
                };
            }

            var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            var pageSize = request.PageSize <= 0 && request.PageSize != -1 ? 10 : (request.PageSize == -1 ? totalItems : request.PageSize);

            // Apply dynamic sorting
            if (!string.IsNullOrEmpty(request.SortColumn))
            {
                var prop = typeof(TEntity).GetProperty(request.SortColumn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    query = BuildOrderBy(query, prop, request.SortOrder);
                }
            }

            // Step 1: Query ONLY the keys for the requested page (Index Scan/Seek)
            var pagedKeys = await query
                .Select(keySelector)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!pagedKeys.Any())
            {
                return new PaginationResponse<TEntity>
                {
                    Data = new List<TEntity>(),
                    TotalCount = totalItems,
                    FilteredCount = totalItems,
                    PaginationInfo = new PaginationResponse<TEntity>.PaginationInfo2
                    {
                        StartItem = 0,
                        EndItem = 0,
                        TotalItems = totalItems,
                        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                        CurrentPage = pageNumber
                    }
                };
            }

            // Step 2: Fetch details for ONLY those matched keys
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TKey));

            var param = keySelector.Parameters[0];
            var keyAccess = keySelector.Body;
            var keysConstant = Expression.Constant(pagedKeys);
            var containsCall = Expression.Call(containsMethod, keysConstant, keyAccess);
            var predicate = Expression.Lambda<Func<TEntity, bool>>(containsCall, param);

            var rawData = await query.Where(predicate).ToListAsync();

            // Maintain exact SQL sort order of pagedKeys
            var keyFunc = keySelector.Compile();
            var orderedData = pagedKeys
                .Select(k => rawData.FirstOrDefault(d => EqualityComparer<TKey>.Default.Equals(keyFunc(d), k)))
                .Where(d => d != null)
                .Cast<TEntity>()
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PaginationResponse<TEntity>
            {
                Data = orderedData,
                TotalCount = totalItems,
                FilteredCount = totalItems,
                PaginationInfo = new PaginationResponse<TEntity>.PaginationInfo2
                {
                    StartItem = (pageNumber - 1) * pageSize + 1,
                    EndItem = Math.Min(pageNumber * pageSize, totalItems),
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber
                }
            };
        }

        private static IQueryable<T> BuildOrderBy<T>(IQueryable<T> query, PropertyInfo prop, string? sortOrder)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var propAccess = Expression.Property(param, prop);
            var lambda = Expression.Lambda(propAccess, param);
            var method = sortOrder?.ToLower() == "asc" ? "OrderBy" : "OrderByDescending";
            var call = Expression.Call(typeof(Queryable), method, new[] { typeof(T), prop.PropertyType },
                query.Expression, Expression.Quote(lambda));
            return query.Provider.CreateQuery<T>(call);
        }
    }
}
