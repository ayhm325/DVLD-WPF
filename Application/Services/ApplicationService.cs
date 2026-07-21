using Application.Interfaces;
using Application.DTOs;
using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _repository;


        public ApplicationService(
            IApplicationRepository repository)
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

                ApplicationStatus =
                    (AppStatus)a.ApplicationStatus,

                LastStatusDate = a.LastStatusDate,

                PaidFees = a.PaidFees,

                CreatedByUserID = a.CreatedByUserID

            }).ToList();
        }





        public async Task<Result<ApplicationBasicInfoDto>> GetBasicInfoAsync(int id)
        {
            var app = await _repository.GetApplicationByIdAsync(id);


            if (app == null)
            {
                return Result<ApplicationBasicInfoDto>.Fail(
                    "Application not found.");
            }


            var dto = new ApplicationBasicInfoDto
            {
                ApplicationID = app.ApplicationID,

                ApplicantPersonID = app.ApplicantPersonID,

                ApplicationStatus =
                    (AppStatus)app.ApplicationStatus,

                PaidFees = app.PaidFees,


                ApplicationTypeName =
                    app.ApplicationType?.ApplicationTypeTitle,


                ApplicantFullName =
                    app.Person != null
                    ? $"{app.Person.FirstName} {app.Person.LastName}"
                    : null,


                ApplicationDate = app.ApplicationDate,


                LastStatusDate = app.LastStatusDate,


                CreatedByUserName =
                    app.CreatedByUser?.UserName
            };


            return Result<ApplicationBasicInfoDto>.Success(dto);
        }







        public async Task<Result<int>> AddNewApplicationAsync(
            ApplicationDto dto)
        {
            if (dto.ApplicantPersonID <= 0)
            {
                return Result<int>.Fail(
                    "Invalid applicant person.");
            }



            var entity = new ApplicationD
            {
                ApplicantPersonID = dto.ApplicantPersonID,

                ApplicationDate = dto.ApplicationDate,

                ApplicationTypeID = dto.ApplicationTypeID,

                ApplicationStatus =
                    (byte)dto.ApplicationStatus,

                LastStatusDate = dto.LastStatusDate,

                PaidFees = dto.PaidFees,

                CreatedByUserID = dto.CreatedByUserID
            };



            var id =
                await _repository.AddNewApplicationAsync(entity);



            return Result<int>.Success(id);
        }








        public async Task<Result<ApplicationDto>> GetApplicationByIdAsync(
            int id)
        {
            var app =
                await _repository.GetApplicationByIdAsync(id);



            if (app == null)
            {
                return Result<ApplicationDto>.Fail(
                    "Application not found.");
            }



            var dto = new ApplicationDto
            {
                ApplicationID = app.ApplicationID,

                ApplicantPersonID = app.ApplicantPersonID,

                ApplicationDate = app.ApplicationDate,

                ApplicationTypeID = app.ApplicationTypeID,

                ApplicationStatus =
                    (AppStatus)app.ApplicationStatus,

                LastStatusDate = app.LastStatusDate,

                PaidFees = app.PaidFees,

                CreatedByUserID = app.CreatedByUserID
            };



            return Result<ApplicationDto>.Success(dto);
        }










        public async Task<Result> UpdateApplicationAsync(
            ApplicationDto dto)
        {
            var entity =
                await _repository.GetApplicationByIdAsync(
                    dto.ApplicationID);



            if (entity == null)
            {
                return Result.Failure(
                    "Application not found.");
            }



            entity.ApplicationStatus =
                (byte)dto.ApplicationStatus;


            entity.PaidFees = dto.PaidFees;


            entity.LastStatusDate =
                dto.LastStatusDate;


            entity.ApplicationTypeID =
                dto.ApplicationTypeID;


            entity.ApplicantPersonID =
                dto.ApplicantPersonID;


            entity.ApplicationDate =
                dto.ApplicationDate;


            entity.CreatedByUserID =
                dto.CreatedByUserID;





            var updated =
                await _repository.UpdateApplicationAsync(entity);




            return updated

                ? Result.Success()

                : Result.Failure(
                    "Application update failed.");
        }









        public async Task<Result> DeleteApplicationAsync(
            int id)
        {
            var app =
                await _repository.GetApplicationByIdAsync(id);



            if (app == null)
            {
                return Result.Failure(
                    "Application not found.");
            }




            if (app.ApplicationStatus ==
                (int)AppStatus.Completed)
            {
                return Result.Failure(
                    "Cannot delete completed application.");
            }






            var deleted =
                await _repository.DeleteApplicationAsync(id);




            return deleted

                ? Result.Success()

                : Result.Failure(
                    "Delete application failed.");
        }









        public async Task<int?> HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId)
        {
            return await _repository.HasDuplicateApplicationAsync(
                personId,
                licenseClassId);
        }









        public async Task<Result> CancelApplicationAsync(
            int applicationId)
        {
            var app =
                await _repository.GetApplicationByIdAsync(
                    applicationId);



            if (app == null)
            {
                return Result.Failure(
                    "Application not found.");
            }




            if (app.ApplicationStatus ==
                (int)AppStatus.Completed)
            {
                return Result.Failure(
                    "Cannot cancel completed application.");
            }





            if (app.ApplicationStatus ==
                (int)AppStatus.Cancelled)
            {
                return Result.Failure(
                    "Application already cancelled.");
            }






            app.ApplicationStatus =
                (byte)AppStatus.Cancelled;



            app.LastStatusDate =
                DateTime.UtcNow;






            var updated =
                await _repository.UpdateApplicationAsync(app);




            return updated

                ? Result.Success()

                : Result.Failure(
                    "Cancel application failed.");
        }









        public async Task<Result> CompleteApplicationAsync(
            int applicationId)
        {
            var app =
                await _repository.GetApplicationByIdAsync(
                    applicationId);




            if (app == null)
            {
                return Result.Failure(
                    "Application not found.");
            }





            if (app.ApplicationStatus ==
                (int)AppStatus.Completed)
            {
                return Result.Failure(
                    "Application already completed.");
            }






            if (app.ApplicationStatus ==
                (int)AppStatus.Cancelled)
            {
                return Result.Failure(
                    "Cannot complete cancelled application.");
            }






            app.ApplicationStatus =
                (byte)AppStatus.Completed;



            app.LastStatusDate =
                DateTime.UtcNow;






            var updated =
                await _repository.UpdateApplicationAsync(app);





            return updated

                ? Result.Success()

                : Result.Failure(
                    "Complete application failed.");
        }
    }
}