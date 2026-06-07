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
                // ... (قم بتعبئة باقي الخصائص كما فعلنا في GetAll)
            };
        }

        public async Task<bool> UpdateApplicationAsync(ApplicationDto dto)
        {
            var entity = await _repository.GetApplicationByIdAsync(dto.ApplicationID);
            if (entity == null) return false;

            // تحديث قيم الـ Entity من الـ DTO
            entity.ApplicationStatus = (byte)dto.ApplicationStatus;
            entity.PaidFees = dto.PaidFees;
            // ... (تحديث باقي الحقول)

            return await _repository.UpdateApplicationAsync(entity);
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            return await _repository.DeleteApplicationAsync(id);
        }
    }
}