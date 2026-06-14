using Application.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly ApplicationRepository _repository;

        public ApplicationService(ApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
        {
            var apps = await _repository.GetAllApplicationsAsync();

            return apps.Select(a => new ApplicationDto
            {
                ApplicationID = a.ApplicationID,
                ApplicantPersonID = a.ApplicantPersonID,
                ApplicationDate = a.ApplicationDate,
                ApplicationTypeID = a.ApplicationTypeID,
                // تحويل صريح من byte (في Entity) إلى AppStatus (في DTO)
                ApplicationStatus = (AppStatus)a.ApplicationStatus,
                LastStatusDate = a.LastStatusDate,
                PaidFees = a.PaidFees,
                CreatedByUserID = a.CreatedByUserID
            }).ToList();
        }

        public async Task<ApplicationBasicInfoDto> GetBasicInfoAsync(int id)
        {
            var app = await _repository.GetApplicationByIdAsync(id);
            if (app == null) throw new Exception("Application not found.");
            return new ApplicationBasicInfoDto
            {

                ApplicationID = app.ApplicationID,
                ApplicantPersonID = app.ApplicantPersonID,                
                ApplicationStatus = (AppStatus)app.ApplicationStatus,
                PaidFees = app.PaidFees,               
                ApplicationTypeName = app.ApplicationType?.ApplicationTypeTitle,               
                ApplicantFullName = app.Person != null ? $"{app.Person.FirstName} {app.Person.LastName}" : null,
                ApplicationDate = app.ApplicationDate,
                LastStatusDate = app.LastStatusDate,
                CreatedByUserName = app.CreatedByUser != null ? $"{app.CreatedByUser.UserName} " : null
            };
        }


        public async Task<int> AddNewApplicationAsync(ApplicationDto dto)
        {
            // تحويل DTO إلى Entity
            var entity = new ApplicationD
            {
                ApplicationID = dto.ApplicationID,
                ApplicantPersonID = dto.ApplicantPersonID,
                ApplicationDate = dto.ApplicationDate,
                ApplicationTypeID = dto.ApplicationTypeID,
                // تحويل صريح من AppStatus (في DTO) إلى byte (في Entity)
                ApplicationStatus = (byte)dto.ApplicationStatus,
                LastStatusDate = dto.LastStatusDate,
                PaidFees = dto.PaidFees,
                CreatedByUserID = dto.CreatedByUserID
            };

            return await _repository.CreateApplicationAsync(entity);
        }

        public async Task<ApplicationDto?> GetApplicationByIdAsync(int id)
        {
            var app = await _repository.GetApplicationByIdAsync(id);
            if (app == null) return null;

            return new ApplicationDto
            {
                ApplicationID = app.ApplicationID,
                ApplicantPersonID = app.ApplicantPersonID,
                ApplicationDate = app.ApplicationDate,
                ApplicationTypeID = app.ApplicationTypeID,
                ApplicationStatus = (AppStatus)app.ApplicationStatus,
                LastStatusDate = app.LastStatusDate,
                PaidFees = app.PaidFees,
                CreatedByUserID = app.CreatedByUserID
            };
        }

        public async Task<bool> UpdateApplicationAsync(ApplicationDto dto)
        {
            var entity = await _repository.GetApplicationByIdAsync(dto.ApplicationID);
            if (entity == null) return false;

            // تحديث قيم الـ Entity من الـ DTO
            entity.ApplicationStatus = (byte)dto.ApplicationStatus;
            entity.PaidFees = dto.PaidFees;
            entity.LastStatusDate = dto.LastStatusDate;
            entity.ApplicationTypeID = dto.ApplicationTypeID;
            entity.ApplicantPersonID = dto.ApplicantPersonID;
            entity.ApplicationDate = dto.ApplicationDate;
            entity.CreatedByUserID = dto.CreatedByUserID;

            return await _repository.UpdateApplicationAsync(entity);
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            // 1. جلب الكيان للتحقق من حالته
            var app = await _repository.GetApplicationByIdAsync(id);
            if (app == null) throw new Exception("Application not found.");

            // 2. الفالديشن الحقيقي (Business Rule)
            if (app.ApplicationStatus == (int)AppStatus.Completed )
            {
                throw new InvalidOperationException("Cannot delete a completed application.");
            }

            // 3. التنفيذ

            return await _repository.DeleteApplicationAsync(id);
        }

        public async Task<int?> HasDuplicateApplicationAsync(int personId,int licenseClassId)
        {
            return await _repository.HasDuplicateApplicationAsync(personId, licenseClassId);
        }


        public async Task<bool> CancelApplicationAsync(int applicationId)
        {
            // 1. جلب الكيان للتحقق من حالته
            var app = await _repository.GetApplicationByIdAsync(applicationId);
            if (app == null) throw new Exception("Application not found.");
            // 2. الفالديشن الحقيقي (Business Rule)
            if (app.ApplicationStatus == (int)AppStatus.Completed)
            {
                throw new InvalidOperationException("Cannot cancel a completed application.");
            }
            // 3. الفالديشن الحقيقي (Business Rule)
            if (app.ApplicationStatus == (int)AppStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot cancel a cancelled application.");
            }
            // 4. التنفيذ
            app.ApplicationStatus = (byte)AppStatus.Cancelled;
            app.LastStatusDate = DateTime.UtcNow;
            return await _repository.UpdateApplicationAsync(app);
        }
    
    
    }
}