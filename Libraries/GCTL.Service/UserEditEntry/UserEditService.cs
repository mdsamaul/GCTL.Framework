using GCTL.Core.Data;
using GCTL.Core.ViewModels.EditUserVM;
using GCTL.Data.Models;
using GCTL.Service.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.UserEditEntry
{
    public class UserEditService : AppService<CoreUserInfo>, IUserEditService
    {
        private readonly IRepository<CoreUserInfo> _entryRepository;

        public UserEditService(IRepository<CoreUserInfo> entryRepository) : base(entryRepository)
        {
            _entryRepository = entryRepository;
        }

        public async Task<EditUserSetupViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var data = await _entryRepository.GetByIdAsync(id);
                if (data == null) return null;

                return new EditUserSetupViewModel
                {
                    UserId = data.Id,
                    Username = data.Username ?? string.Empty,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    EmployeeId = data.EmployeeId,
                    Dob = data.Dob,
                    OffPhone = data.OffPhone,
                    PerPhone = data.PerPhone,
                    OffEmail = data.OffEmail,
                    PerEmail = data.PerEmail,
                    WorkStation = data.WorkStation,
                    Ldate = data.Ldate
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<EditUserSetupViewModel?> GetByIdAsync(string userName)
        {
            try
            {
                var data = await _entryRepository.All().AsNoTracking().FirstOrDefaultAsync(x => x.Username == userName);
                if (data == null) return null;

                return new EditUserSetupViewModel
                {
                    UserId = data.Id,
                    Username = data.Username ?? string.Empty,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    EmployeeId = data.EmployeeId,
                    Dob = data.Dob,
                    OffPhone = data.OffPhone,
                    PerPhone = data.PerPhone,
                    OffEmail = data.OffEmail,
                    PerEmail = data.PerEmail,
                    WorkStation = data.WorkStation,
                    Ldate = data.Ldate
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<(List<EditUserGridViewModel> Data, int totalRecord, int currentRecord)> GetPaginatedDataAsync(
            string searchValue, int page, int pageSize, string sortColumn, string sortDirection, string userName)
        {
            var query = await (from e in _entryRepository.All().AsNoTracking()
                               where e.Username == userName
                               select new EditUserGridViewModel
                               {
                                   UserId = e.Id,
                                   Username = e.Username ?? string.Empty,
                                   FullName = ((e.FirstName ?? "") + " " + (e.LastName ?? "")).Trim(),
                                   UserType = e.Type ?? e.UserType ?? "User",
                                   EntryDate = e.EntryDate.HasValue
                                        ? e.EntryDate.Value.ToString("dd/MM/yyyy")
                                        : (e.Ldate.HasValue ? e.Ldate.Value.ToString("dd/MM/yyyy") : string.Empty)
                               }).ToListAsync();

            var totalRecord = query.Count;
            IEnumerable<EditUserGridViewModel> filterQuery = query;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                filterQuery = filterQuery.Where(x =>
                    (x.Username?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.FullName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.UserType?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.EntryDate?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.UserId.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                );
            }

            var currentRecord = filterQuery.Count();

            if (!string.IsNullOrWhiteSpace(sortColumn) && !string.IsNullOrWhiteSpace(sortDirection))
            {
                filterQuery = sortColumn.ToLower() switch
                {
                    "username" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(x => x.Username) : filterQuery.OrderByDescending(x => x.Username),
                    "fullname" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(x => x.FullName) : filterQuery.OrderByDescending(x => x.FullName),
                    "usertype" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(x => x.UserType) : filterQuery.OrderByDescending(x => x.UserType),
                    "entrydate" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(x => x.EntryDate) : filterQuery.OrderByDescending(x => x.EntryDate),
                    "userid" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(x => x.UserId) : filterQuery.OrderByDescending(x => x.UserId),
                    _ => filterQuery.OrderBy(a => a.UserId)
                };
            }
            else
            {
                filterQuery = filterQuery.OrderBy(a => a.UserId);
            }

            var data = pageSize < 0 ? filterQuery.ToList() : filterQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (data, totalRecord, currentRecord);
        }

        public async Task<(bool isSuccess, string message, object? data)> SaveAsync(EditUserSetupViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Username))
            {
                return (false, "Invalid user request.", null);
            }

            await _entryRepository.BeginTransactionAsync();

            try
            {
                var exData = model.UserId > 0
                    ? await _entryRepository.GetByIdAsync(model.UserId)
                    : await _entryRepository.All().FirstOrDefaultAsync(x => x.Username == model.Username);

                if (exData == null)
                    return (false, "User does not exist.", null);

                exData.Luser = model.Luser;
                exData.Lip = model.Lip;
                exData.Lmac = model.Lmac;
                exData.Ldate = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var hasher = new PasswordHasher<CoreUserInfo>();
                    exData.Password = hasher.HashPassword(exData, model.Password);
                }

                await _entryRepository.UpdateAsync(exData);
                await _entryRepository.CommitTransactionAsync();

                return (true, "Password updated successfully.", exData);
            }
            catch (Exception ex)
            {
                await _entryRepository.RollbackTransactionAsync();
                return (false, "Failed to update profile: " + ex.Message, null);
            }
        }
    }
}

