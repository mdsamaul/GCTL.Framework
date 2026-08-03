using GCTL.Core.DataTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCTL.Service.Pagination
{
    #region new Pagination
    public class PaginationService<TEntity>
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _stringProps = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _intProps = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _decimalProps = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _dateProps = new();

        private static PropertyInfo[] Searchable(Type t, Type propType) =>
            t.GetProperties()
            .Where(p => p.PropertyType == propType && p.GetCustomAttribute<SearchableAttribute>() != null)
            .ToArray();

        public static async Task<PaginationResponse<TEntity>> GetPaginatedData(
        IQueryable<TEntity> query, PaginationRequest request)
        {
            try
            {
                var type = typeof(TEntity);
                var stringProps = _stringProps.GetOrAdd(type, t => Searchable(t, typeof(string)));
                var intProps = _intProps.GetOrAdd(type, t => Searchable(t, typeof(int)).Concat(Searchable(t, typeof(int?))).ToArray());
                var decimalProps = _decimalProps.GetOrAdd(type, t => Searchable(t, typeof(decimal)).Concat(Searchable(t, typeof(decimal?))).ToArray());
                var dateProps = _dateProps.GetOrAdd(type, t => Searchable(t, typeof(DateTime)).Concat(Searchable(t, typeof(DateTime?))).ToArray());
                var totalItems = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim();
                    Console.WriteLine(query);
                    var predicate = BuildSearchPredicate<TEntity>(term, stringProps, intProps, decimalProps, dateProps);
                    if (predicate != null)
                        query = query.Where(predicate);
                    Console.WriteLine(query);
                }

                if (!string.IsNullOrEmpty(request.SortColumn))
                {
                    var prop = type.GetProperty(request.SortColumn, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                        query = BuildOrderBy(query, prop, request.SortOrder);
                }

                var fileredCount = await query.CountAsync();

                var data = request.PageSize > 0
                    ? await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync()
                    : await query.ToListAsync();

                var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)fileredCount / request.PageSize) : 0;

                return new PaginationResponse<TEntity>
                {
                    Data = data,
                    TotalCount = totalItems,
                    FilteredCount = fileredCount,
                    PaginationInfo = new PaginationResponse<TEntity>.PaginationInfo2
                    {
                        StartItem = totalItems == 0 ? 0 : (request.PageNumber - 1) * request.PageSize + 1,
                        EndItem = Math.Min(request.PageNumber * request.PageSize, totalItems),
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        CurrentPage = request.PageNumber
                    }
                };
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        private static Expression<Func<T, bool>> BuildSearchPredicate<T>(
        string term, PropertyInfo[] stringProps, PropertyInfo[] intProps,
        PropertyInfo[] decimalProps, PropertyInfo[] dateProps)
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression body = null;

            // String columns: EF.Functions.Like — indexable with proper collation/index
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) });
            var efFunctions = Expression.Constant(EF.Functions);
            var pattern = Expression.Constant($"%{term}%");

            foreach (var p in stringProps)
            {
                var propAccess = Expression.Property(param, p);
                var safe = Expression.Coalesce(propAccess, Expression.Constant(""));
                var call = Expression.Call(likeMethod, efFunctions, safe, pattern);
                body = body == null ? call : Expression.OrElse(body, call);
            }

            // Numeric columns: exact equality — only if term parses cleanly, keeps it indexable
            if (int.TryParse(term, out var intVal))
                foreach (var p in intProps)
                    body = OrEquals(body, param, p, intVal);

            if (decimal.TryParse(term, out var decVal))
                foreach (var p in decimalProps)
                    body = OrEquals(body, param, p, decVal);

            // Date columns: exact day match
            if (TryParseAnyDate(term, out var dateVal))
                foreach (var p in dateProps)
                    body = OrDateEquals(body, param, p, dateVal.Date);

            return body == null ? null : Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static Expression OrEquals(Expression body, ParameterExpression param, PropertyInfo p, object value)
        {
            var propAccess = Expression.Property(param, p);
            var underlyingType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            var constant = Expression.Constant(Convert.ChangeType(value, underlyingType), underlyingType);
            Expression equals = p.PropertyType != underlyingType
                ? Expression.Equal(propAccess, Expression.Convert(constant, p.PropertyType))
                : Expression.Equal(propAccess, constant);
            return body == null ? equals : Expression.OrElse(body, equals);
        }

        private static Expression OrDateEquals(Expression body, ParameterExpression param, PropertyInfo p, DateTime date)
        {
            var propAccess = Expression.Property(param, p);
            var isNullable = Nullable.GetUnderlyingType(p.PropertyType) != null;
            Expression dateOnly = isNullable
                ? Expression.Property(Expression.Property(propAccess, "Value"), "Date")
                : Expression.Property(propAccess, "Date");
            var constant = Expression.Constant(date, typeof(DateTime));
            Expression equals = Expression.Equal(dateOnly, constant);
            if (isNullable)
            {
                var hasValue = Expression.Property(propAccess, "HasValue");
                equals = Expression.AndAlso(hasValue, equals);
            }
            return body == null ? equals : Expression.OrElse(body, equals);
        }

        private static IQueryable<T> BuildOrderBy<T>(IQueryable<T> query, PropertyInfo prop, string sortOrder)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var propAccess = Expression.Property(param, prop);
            var lambda = Expression.Lambda(propAccess, param);
            var method = sortOrder?.ToLower() == "asc" ? "OrderBy" : "OrderByDescending";
            var call = Expression.Call(typeof(Queryable), method, new[] { typeof(T), prop.PropertyType },
                query.Expression, Expression.Quote(lambda));
            return query.Provider.CreateQuery<T>(call);
        }

        private static readonly string[] _dateFormats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd/M/yy", "d/M/yy",
            "MM/dd/yyyy", "M/d/yyyy", "MM/dd/yy", "M/d/yy",
            "dd-MM-yyyy", "d-M-yyyy", "dd-M-yy", "d-M-yy",
            "MM-dd-yyyy", "M-d-yyyy",
            "yyyy-MM-dd", "yyyy-M-d", "yy-M-dd", "yyyy/MM/dd", "yyyy/M/d", "yy/M/dd",
            "dd.MM.yyyy", "d.M.yyyy", "yyyy.MM.dd",
            "dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "d MMMM yyyy",
            "MMM dd, yyyy", "MMM d, yyyy", "MMMM dd, yyyy", "MMMM d, yyyy",
            "dd-MMM-yyyy", "dd-MMM-yy"
        };

        private static bool TryParseAnyDate(string term, out DateTime result)
        {
            // 3. Explicit format list — catches anything ambiguous the above two rejected
            if (DateTime.TryParseExact(term, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            // 1. Loose parse, invariant culture — catches most natural typed dates
            if (DateTime.TryParse(term, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            // 2. Loose parse, current/server culture — catches locale-specific input
            if (DateTime.TryParse(term, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                return true;



            result = default;
            return false;
        }
    }
    #endregion

    public class PaginationService<TEntity, TViewModel> where TEntity : class
    {
        #region PaginationResult & PaginationInfo Class
        public class PaginationResult<T>
        {
            public IEnumerable<T> Data { get; set; }
            public int TotalCount { get; set; }
            public PaginationInfo PaginationInfo { get; set; }
        }

        public class PaginationInfo
        {
            public int StartItem { get; set; }
            public int EndItem { get; set; }
            public int TotalItems { get; set; }
            public List<int> PageNumbers { get; set; }
            public int TotalPages { get; set; }
            public int CurrentPage { get; set; }
        }
        #endregion


        #region GetPaginatedData
        public static async Task<PaginationResult<TViewModel>> GetPaginatedData(IQueryable<TEntity> query,
            int pageNumber, int pageSize, string searchTerm, string sortColumn, string sortOrder,
            Func<string, Expression<Func<TEntity, bool>>> searchPredicate, Func<TEntity, TViewModel> selector)
        {
            // Searching
            if (!string.IsNullOrEmpty(searchTerm) && searchPredicate != null)
            {
                searchTerm = Regex.Replace(searchTerm, @"\s+", " ").Trim().ToLower();
                query = query.Where(searchPredicate(searchTerm));
            }

            // Sorting based on the column and direction
            if (!string.IsNullOrEmpty(sortColumn))
            {
                var propertyInfo = typeof(TEntity).GetProperty(sortColumn);
                if (propertyInfo != null)
                {
                    query = sortOrder.ToLower() == "asc" ? query.OrderBy(e => EF.Property<object>(e, sortColumn)) : query.OrderByDescending(e => EF.Property<object>(e, sortColumn));
                }
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Apply pagination 
            var paginatedData = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(selector).ToList();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var startItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1;
            var endItem = Math.Min(pageNumber * pageSize, totalItems);
            var pageNumbers = Enumerable.Range(1, totalPages).ToList();

            return new PaginationResult<TViewModel>
            {
                Data = paginatedData,
                TotalCount = totalItems,
                PaginationInfo = new PaginationInfo
                {
                    StartItem = startItem,
                    EndItem = endItem,
                    TotalItems = totalItems,
                    PageNumbers = pageNumbers,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber
                }
            };
        }
        #endregion
    }


    #region SeparatePaginationResult & SeparatePaginationInfo Class
    public class SeparatePaginationResult<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
        public SeparatePaginationInfo SeparatePaginationInfo { get; set; }
    }

    public class SeparatePaginationInfo
    {
        public int StartItem { get; set; }
        public int EndItem { get; set; }
        public int TotalItems { get; set; }
        public List<int> PageNumbers { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
    #endregion
}
