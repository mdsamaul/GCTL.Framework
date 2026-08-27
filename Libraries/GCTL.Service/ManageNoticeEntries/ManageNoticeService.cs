using GCTL.Core.Data;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Email;
using GCTL.Core.ViewModels.ManageNoticeEntries;
using GCTL.Data.Models;
using GCTL.Service.DeleteHistories;
using GCTL.Service.EmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;


namespace GCTL.Service.ManageNoticeEntries
{
    public class ManageNoticeService : AppService<CoreNotice>, IManageNoticeService
    {
        private readonly IHostEnvironment hostEnvironment;

        private readonly IRepository<CoreNotice> entryRepo;
        private readonly IRepository<NoticeDocumentFile> noticeDocRepo;
        private readonly IRepository<CoreCompany> comRepo;
        private readonly IRepository<HrmDefDesignation> desiRepo;
        private readonly IRepository<HrmEmployee> empRepo;
        private readonly IRepository<HrmEmployeeOfficialInfo> empOffRepo;
        private readonly IRepository<HrmDefDepartment> depRepo;
        private readonly IRepository<CoreBranch> branchRepo;
        private readonly IRepository<HrmDefEmpType> empTypeRepo;
        private readonly IRepository<HrmEisDefEmploymentNature> empNatureRepo;
        private readonly IRepository<HrmDefEmployeeStatus> empStatusRepo;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IDeleteHistoryService deleteHistoryService;

        private readonly IEmailService emailService;

        public ManageNoticeService(
            IHostEnvironment hostEnvironment,
            IEmailService emailService,
            IRepository<NoticeDocumentFile> noticeDocRepo,
            IRepository<CoreNotice> entryRepo,
            IRepository<CoreCompany> comRepo,
            IRepository<HrmDefDesignation> desiRepo,
            IRepository<HrmEmployee> empRepo,
            IRepository<HrmEmployeeOfficialInfo> empOffRepo,
            IRepository<HrmDefDepartment> depRepo,
            IRepository<CoreBranch> branchRepo,
            IRepository<HrmDefEmpType> empTypeRepo,
            IRepository<HrmEisDefEmploymentNature> empNatureRepo,
            IRepository<HrmDefEmployeeStatus> empStatusRepo,
            IRepository<CoreAccessCode> accessCodeRepository,
            IDeleteHistoryService deleteHistoryService) : base(entryRepo)
        {
            this.hostEnvironment = hostEnvironment;
            this.entryRepo = entryRepo;
            this.noticeDocRepo = noticeDocRepo;
            this.comRepo = comRepo;
            this.desiRepo = desiRepo;
            this.empRepo = empRepo;
            this.empOffRepo = empOffRepo;
            this.depRepo = depRepo;
            this.branchRepo = branchRepo;
            this.empTypeRepo = empTypeRepo;
            this.empNatureRepo = empNatureRepo;
            this.empStatusRepo = empStatusRepo;
            this.emailService = emailService;
            this.accessCodeRepository = accessCodeRepository;
            this.deleteHistoryService = deleteHistoryService;
        }

        public async Task<bool> BulkDeleteAsync(List<int> tcs, DeleteHistoryViewModel model)
        {
            if (tcs.Count == 0) return false;

            const int batchSize = 1000;
            var nTableName = entryRepo.GetTableName();
            var fTableName = noticeDocRepo.GetTableName();
            var deletedEntries = new List<CoreNotice>();
            var deletedFiles = new List<NoticeDocumentFile>();

            await entryRepo.BeginTransactionAsync();

            try
            {
                for (int i = 0; i < tcs.Count; i += batchSize)
                {
                    var batch = tcs.Skip(i).Take(batchSize).ToList();

                    var entries = await (from e in entryRepo.All().AsNoTracking()
                                         join n in noticeDocRepo.All().AsNoTracking() on e.NoticeId equals n.NoticeId into nGroup
                                         from n in nGroup.DefaultIfEmpty()
                                         where batch.Contains(e.Tc)
                                         select new { e, n }
                                         ).ToListAsync();

                    var entryEntities = new List<CoreNotice>();
                    var noticeEntities = new List<NoticeDocumentFile>();

                    if (entries.Any())
                    {
                        foreach (var entry in entries)
                        {
                            if ( !string.IsNullOrEmpty(entry.n?.ImgType))
                            {
                                DeleteFile(entry.n.ImgType);
                                noticeEntities.Add(entry.n);
                                deletedFiles.Add(entry.n);
                            }

                            entryEntities.Add(entry.e);
                            deletedEntries.Add(entry.e);
                        }

                        if (noticeEntities.Any())
                        {
                            await noticeDocRepo.DeleteRangeAsync(noticeEntities);
                        }

                        if (entryEntities.Any())
                        {
                            await entryRepo.DeleteRangeAsync(entryEntities);
                        }
                    }
                }

                model.tableName = nTableName;
                await deleteHistoryService.LogDeletedRecordsAsync(deletedEntries, model);

                model.tableName = fTableName;
                await deleteHistoryService.LogDeletedRecordsAsync(deletedFiles, model);

                await entryRepo.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await entryRepo.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> EditAsync(ManageNoticeSetupViewModel model)
        {
            if (model == null || model.Tc == 0 || model.NoticeId == null)
            {
                return false;
            }

            await entryRepo.BeginTransactionAsync();

            try
            {
                var exRecord = await entryRepo.GetByIdAsync(model.Tc);

                if (exRecord == null)
                    return false;

                var existingDoc = await noticeDocRepo.All().FirstOrDefaultAsync(d => d.NoticeId == exRecord.NoticeId);

                if (model.formFile != null && model.formFile.Length > 0)
                {
                    if (existingDoc != null && !string.IsNullOrEmpty(existingDoc.ImgType))
                    {
                        DeleteFile(existingDoc.ImgType);
                        await noticeDocRepo.DeleteAsync(existingDoc.Tc);
                    }

                    string newFile = await SaveFileAsync(model.formFile, model.NoticeId);
                }

                if (model.Status?.Trim() == "1")
                {
                    var existingActiveRecords = await entryRepo.All().Where(x => x.Status.Trim() == "1").ToListAsync();

                    foreach (var existingRecord in existingActiveRecords)
                    {
                        existingRecord.Status = "0";
                        await entryRepo.UpdateAsync(existingRecord);
                    }
                }

                exRecord.NoticeId = model.NoticeId;
                exRecord.NoticeTitle = model.NoticeTitle;
                exRecord.NoticeDesc = model.NoticeDesc ?? string.Empty;
                exRecord.Status = model.Status;
                exRecord.PriorityLevel = model.PriorityLevel;

                exRecord.Luser = model.Luser;
                exRecord.Lip = model.Lip;
                exRecord.Lmac = model.Lmac;
                exRecord.ModifyDate = model.ModifyDate;

                await entryRepo.UpdateAsync(exRecord);

                await entryRepo.CommitTransactionAsync();

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database Update Error: {dbEx.Message}");
                if (dbEx.InnerException != null) Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return false;
            }
        }

        public async Task<string> GenerateIdAsync()
        {
            try
            {
                var maxId = entryRepo.All().AsNoTracking().AsEnumerable()
                    .Where(e => !string.IsNullOrEmpty(e.NoticeId) && e.NoticeId.All(char.IsDigit))
                    .Select(e => int.Parse(e.NoticeId))
                    .DefaultIfEmpty(0)
                    .Max();
                return (maxId + 1).ToString("D4");
            }
            catch
            {
                return "0001";
            }
        }

        public async Task<ManageNoticeSetupViewModel> GetByIdAsync(int id)
        {
            var service = await entryRepo.GetByIdAsync(id);

            if (entryRepo == null) return null;

            var model = new ManageNoticeSetupViewModel()
            {
                Tc = service.Tc,
                NoticeId = service.NoticeId,
                NoticeTitle = service.NoticeTitle,
                NoticeDesc = service.NoticeDesc,
                Status = service.Status.Trim(),
                EntryDate = service.EntryDate,
                Ldate = service.Ldate,
                Luser = service.Luser,
                ModifyDate = service.ModifyDate,
                PriorityLevel = service.PriorityLevel
            };
            return model;
        }

        public async Task<EmployeeFilterResultDto> GetFilterEmpAsync(EmployeeFilterViewModel filter)
        {
            var result = new EmployeeFilterResultDto();

            var query = from e in empOffRepo.All().AsNoTracking()
                        join emp in empRepo.All().AsNoTracking() on e.EmployeeId equals emp.EmployeeId
                        join c in comRepo.All().AsNoTracking() on e.CompanyCode equals c.CompanyCode
                        join b in branchRepo.All().AsNoTracking() on e.BranchCode equals b.BranchCode into branchGroup
                        from b in branchGroup.DefaultIfEmpty()
                        join d in depRepo.All().AsNoTracking() on e.DepartmentCode equals d.DepartmentCode into deptGroup
                        from d in deptGroup.DefaultIfEmpty()
                        join ds in desiRepo.All().AsNoTracking() on e.DesignationCode equals ds.DesignationCode into desigGroup
                        from ds in desigGroup.DefaultIfEmpty()
                        join status in empStatusRepo.All().AsNoTracking() on e.EmployeeStatus equals status.EmployeeStatusId into statusGroup
                        from status in statusGroup.DefaultIfEmpty()
                        join emptype in empTypeRepo.All().AsNoTracking() on e.EmpTypeCode equals emptype.EmpTypeCode into empTypeGroup
                        from emptype in empTypeGroup.DefaultIfEmpty()
                        join empNature in empNatureRepo.All().AsNoTracking() on e.EmploymentNatureId equals empNature.EmploymentNatureId into empNatureGroup
                        from empNature in empNatureGroup.DefaultIfEmpty()

                        where (e.EmployeeStatus == "01")

                        select new
                        {
                            EmployeeId = e.EmployeeId,
                            FirstName = emp.FirstName,
                            LastName = emp.LastName,
                            e.JoiningDate,
                            e.CompanyCode,
                            CompanyName = c.CompanyName,
                            e.BranchCode,
                            BranchName = b.BranchName,
                            e.DepartmentCode,
                            DepartmentName = d.DepartmentName,
                            e.DesignationCode,
                            DesignationName = ds.DesignationName,
                            EmployeeTypeName = emptype.EmpTypeName,
                            EmployeeStatusId = e.EmployeeStatus,
                            EmployeeStatusName = status.EmployeeStatus,
                            EmpNatureCode = e.EmploymentNatureId,
                            EmpNatureName = empNature.EmploymentNature
                        };

            if (filter.BranchCodes?.Any() == true)
                query = query.Where(x => filter.BranchCodes.Contains(x.BranchCode));

            if (filter.DepartmentCodes?.Any() == true)
                query = query.Where(x => filter.DepartmentCodes.Contains(x.DepartmentCode));

            if (filter.DesignationCodes?.Any() == true)
                query = query.Where(x => filter.DesignationCodes.Contains(x.DesignationCode));

            if (filter.EmployeeCodes?.Any() == true)
                query = query.Where(x => filter.EmployeeCodes.Contains(x.EmployeeId));

            var filteredData = await query.ToListAsync();

            var company = await comRepo.All().AsNoTracking()
                .Select(c => new LookupItemDto
                {
                    Code = c.CompanyCode,
                    Name = c.CompanyName
                }).ToListAsync();

            var branch = await branchRepo.All().AsNoTracking()
                .Select(c => new LookupItemDto
                {
                    Code = c.BranchCode,
                    Name = c.BranchName
                }).ToListAsync();

            result.Employees = filteredData.Select(x => new EmployeeListItemViewModel
            {
                EmployeeId = x.EmployeeId,
                EmployeeName = string.Join(" ", new[] { x.FirstName, x.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                JoiningDate = x.JoiningDate.HasValue ? x.JoiningDate.Value.ToString("dd/MM/yyyy") : "",
                DesignationName = x.DesignationName ?? "",
                DepartmentName = x.DepartmentName ?? "",
                BranchName = x.BranchName ?? "",
                CompanyName = x.CompanyName ?? "",
                EmployeeTypeName = x.EmployeeTypeName ?? "",
                EmployeeStatus = x.EmployeeStatusName ?? "",
                EmploymentNature = x.EmpNatureName ?? " "
            }).ToList();

            result.LookupData["companies"] = company;
            result.LookupData["branches"] = branch;

            result.LookupData["employees"] = filteredData
                .Where(x => x.EmployeeId != null)
                .Select(x => new LookupItemDto
                {
                    Code = x.EmployeeId,
                    Name = string.Join(" ", new[] { x.FirstName, x.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)))
                })
                .Distinct()
                .ToList();

            result.LookupData["designations"] = filteredData
                .Where(x => x.DesignationCode != null && x.DesignationName != null)
                .Select(x => new LookupItemDto { Code = x.DesignationCode, Name = x.DesignationName })
                .Distinct()
                .ToList();

            result.LookupData["departments"] = filteredData
                .Where(x => x.DepartmentCode != null && x.DepartmentName != null)
                .Select(x => new LookupItemDto { Code = x.DepartmentCode, Name = x.DepartmentName })
                .Distinct()
                .ToList();

            return result;
        }

        public async Task<(List<ManageNoticeSetupViewModel> Data, int TotalRecords, int FilterRecords)> GetPaginatedDataAsync(string searchValue, int page, int pageSize, string sortColumn, string sortDirection)
        {
            var query = from m in entryRepo.All().AsNoTracking()
                        join d in noticeDocRepo.All().AsNoTracking()
                        on m.NoticeId equals d.NoticeId into entryGroup
                        from d in entryGroup.DefaultIfEmpty()
                        select new ManageNoticeSetupViewModel
                        {
                            Tc = m.Tc,
                            NoticeId = m.NoticeId,
                            NoticeTitle = m.NoticeTitle,
                            NoticeDesc = m.NoticeDesc,
                            EntryDate = m.EntryDate,
                            PriorityLevel = m.PriorityLevel,
                            Status = m.Status.Trim(),
                            FilePath = d.ImgType,
                        };

            var materializedQuery = await query.ToListAsync();

            IEnumerable<ManageNoticeSetupViewModel> filterQuery = materializedQuery;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                filterQuery = filterQuery.Where(d =>
                    (d.NoticeId?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.NoticeTitle?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.NoticeDesc?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.PriorityLevel?.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.EntryDate?.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Status?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            var totalRecords = materializedQuery.Count();
            var filteredRecords = filterQuery.Count();

            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortDirection))
            {
                filterQuery = sortColumn.ToLower() switch
                {
                    "noticeid" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.NoticeId) : filterQuery.OrderByDescending(a => a.NoticeId),
                    "noticetitle" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.NoticeTitle) : filterQuery.OrderByDescending(a => a.NoticeTitle),
                    "noticedesc" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.NoticeDesc) : filterQuery.OrderByDescending(a => a.NoticeDesc),
                    "status" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.Status) : filterQuery.OrderByDescending(a => a.Status),
                    "entrydate" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.EntryDate) : filterQuery.OrderByDescending(a => a.EntryDate),
                    "prioritylevel" => sortDirection.ToLower() == "asc" ? filterQuery.OrderBy(a => a.PriorityLevel) : filterQuery.OrderByDescending(a => a.PriorityLevel),
                    _ => filterQuery.OrderBy(a => a.Tc)
                };
            }
            else
            {
                filterQuery = filterQuery.OrderByDescending(a => a.Tc);
            }

            var data = pageSize < 0
                ? filterQuery.ToList()
                : filterQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (data, totalRecords, filteredRecords);
        }

        public async Task<bool> SaveAsync(ManageNoticeSetupViewModel model)
        {
            if (model == null || model.Tc != 0)
                return false;

            await entryRepo.BeginTransactionAsync();

            try
            {
                var newId = await GenerateIdAsync();

                if (model.Status?.Trim() == "1")
                {
                    var existingActiveRecords = await entryRepo.All().Where(x => x.Status.Trim() == "1").ToListAsync();

                    foreach (var existingRecord in existingActiveRecords)
                    {
                        existingRecord.Status = "0";
                        await entryRepo.UpdateAsync(existingRecord);
                    }
                }

                string fileName = null;
                if (model.formFile != null && model.formFile.Length > 0)
                    await SaveFileAsync(model.formFile, newId);

                CoreNotice record = new CoreNotice
                {
                    NoticeId = newId,
                    NoticeTitle = model.NoticeTitle,
                    NoticeDesc = model.NoticeDesc ?? string.Empty,
                    EntryDate = DateTime.Now.Date,
                    PriorityLevel = model.PriorityLevel,
                    Status = model.Status.Trim(),
                    //FilePath = fileName,
                    Ldate = model.Ldate,
                    Lip = model.Lip,
                    Lmac = model.Lmac,
                    Luser = model.Luser,
                };

                await entryRepo.AddAsync(record);
                await entryRepo.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await entryRepo.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> SentNoticeToEmployeeAsync(List<string> empIds, List<int> tcs)
        {
            if (empIds?.Count == 0 || tcs?.Count == 0) return false;

            try
            {
                var employee = await (from emp in empRepo.All().AsNoTracking()
                                      join e in empOffRepo.All().AsNoTracking() on emp.EmployeeId equals e.EmployeeId
                                      where empIds.Contains(e.EmployeeId) && e.EmployeeStatus == "01"
                                      select new
                                      {
                                          EmployeeId = e.EmployeeId,
                                          FirstName = emp.FirstName,
                                          LastName = emp.LastName,
                                          Email = e.Email
                                      }).ToListAsync();

                var notices = await (from e in entryRepo.All().AsNoTracking()
                                     join n in noticeDocRepo.All().AsNoTracking() on e.NoticeId equals n.NoticeId into nGroup
                                     from n in nGroup.DefaultIfEmpty()
                                     select new { e, n }
                                     )
                    .Where(x => tcs.Contains(x.e.Tc))
                    .ToListAsync();

                if (!employee.Any() || !notices.Any()) return false;

                //var emailTasks = new List<Task>();

                var request = new List<EmailDataDto>();
                var empWithValidEmails = employee.Where(emp => !string.IsNullOrEmpty(emp.Email)).ToList();

                foreach (var emp in empWithValidEmails)
                {
                    var empName = string.Join(" ", new[] { emp.FirstName, emp.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));

                    foreach (var notice in notices)
                    {
                        var emailBody = CreateEmailBody(notice.e, empName);
                        string attachmentPath = null;
                        string attachmentName = null;

                        if (notice.n != null && !string.IsNullOrEmpty(notice.n.ImgType))
                        {
                            attachmentPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "NoticeDocument", notice.n.ImgType);
                            attachmentName = notice.n.ImgType;
                        }


                        if (!File.Exists(attachmentPath))
                        {
                            attachmentPath = null;
                            attachmentName = null;
                        }

                        request.Add(new EmailDataDto
                        {
                            To = emp.Email,
                            Subject = notice.e.NoticeTitle,
                            Body = emailBody,
                            AttachmentName = attachmentName,
                            AttachmentPath = attachmentPath
                        });
                    }
                }
                if (!request.Any())
                    return false;

                const int batchSize = 85; // Adjust batch size as needed
                int totalEmails = request.Count;
                int processedEmails = 0;

                for (int i = 0; i < totalEmails; i += batchSize)
                {
                    var batch = request.Skip(i).Take(batchSize).ToList();

                    try
                    {
                        await emailService.SendBatchEmailsAsync(batch);

                        //Delay for 5 second between batches
                        //Task.Delay(5000).Wait();

                        processedEmails += batch.Count;
                        Console.WriteLine($"Sent {processedEmails}/{totalEmails} emails.");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending batch emails: {ex.Message}");
                    }
                }
                //await Task.WhenAll(emailTasks);

                //await emailService.SendBatchEmailsAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notice emails: {ex.Message}");
                return false;
            }
        }
        private static string CreateEmailBody(CoreNotice notice, string empName)
        {
            return $@"<!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ background: #f9f9f9; padding: 20px; border-radius: 8px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <p>Dear {empName},</p>
                    <p>{notice.NoticeDesc}</p>
                    
                    <p>Regards,<br/>The HR Team</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> SaveFileAsync(IFormFile file, string noticeId = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;

                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                //var noticeDoc = new NoticeDocumentFile
                //{
                //    NoticeId = noticeId,
                //    Photo = fileData,
                //    ImgType = file.ContentType,
                //    ImgSize = file.Length
                //};

                //await noticeDocRepo.AddAsync(noticeDoc);

                string uploadsFolder = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "NoticeDocument");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(file.FileName);
                string fileName = $"{noticeId}_{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var noticeDoc = new NoticeDocumentFile
                {
                    NoticeId = noticeId,
                    Photo = null,
                    ImgType = fileName,
                    ImgSize = file.Length
                };

                await noticeDocRepo.AddAsync(noticeDoc);

                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File save error: {ex.Message}");
                return null;
            }
        }
        private void DeleteFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return;

                string filePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "NoticeDocument", fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File delete error: {ex.Message}");
            }
        }

        public string GetFileUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            return $"/File/{fileName}";
        }




        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title.Trim() == "Manage Notice" && x.CheckDelete);
        }
        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title.Trim() == "Manage Notice" && x.CheckEdit);
        }
        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title.Trim() == "Manage Notice" && x.CheckAdd);
        }
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title.Trim() == "Manage Notice" && x.TitleCheck);
        }
    }
}
