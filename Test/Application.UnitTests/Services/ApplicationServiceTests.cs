using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public sealed class ApplicationServiceTests
{
    private readonly Mock<IApplicationRepository> _repository = new();
    private readonly Mock<IApplicationTypeRepository> _applicationTypeRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private ApplicationService CreateService() =>
        new(
            _repository.Object,
            _applicationTypeRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object);

    private static ApplicationD CreateApplication(
        int id = 1,
        int applicantPersonId = 100,
        int applicationTypeId = 1,
        AppStatus status = AppStatus.New,
        decimal paidFees = 50m)
    {
        var now = DateTime.UtcNow;

        return new ApplicationD
        {
            ApplicationID = id,
            ApplicantPersonID = applicantPersonId,
            ApplicationDate = now.AddDays(-1),
            ApplicationTypeID = applicationTypeId,
            ApplicationStatus = status,
            LastStatusDate = now.AddHours(-1),
            PaidFees = paidFees,
            CreatedByUserID = 10
        };
    }

    private static ApplicationType CreateApplicationType(
        int id = 1,
        decimal fees = 50m,
        string title = "Test Application")
        => new()
        {
            ApplicationTypeId = id,
            ApplicationFees = fees,
            ApplicationTypeTitle = title
        };

    private static CreateApplicationDto ValidCreateDto(
        int applicantPersonId = 100,
        int applicationTypeId = 1)
        => new()
        {
            ApplicantPersonID = applicantPersonId,
            ApplicationTypeID = applicationTypeId
        };

    private static UpdateApplicationDto ValidUpdateDto(
        int applicationId = 1,
        int applicationTypeId = 2)
        => new()
        {
            ApplicationID = applicationId,
            ApplicationTypeID = applicationTypeId
        };

    private void SetupAuthenticatedUser(
        int userId = 10)
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(userId);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllApplicationsAsync_WhenRepositoryReturnsApplications_ReturnsMappedDtos()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync(
            [
                CreateApplication(1, 100, 1, AppStatus.New, 50m),
                CreateApplication(2, 101, 2, AppStatus.Completed, 75m)
            ]);

        var result =
            await service.GetAllApplicationsAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(1, result.Value[0].ApplicationID);
        Assert.Equal(100, result.Value[0].ApplicantPersonID);
        Assert.Equal(1, result.Value[0].ApplicationTypeID);
        Assert.Equal(AppStatus.New, result.Value[0].ApplicationStatus);
        Assert.Equal(50m, result.Value[0].PaidFees);

        Assert.Equal(2, result.Value[1].ApplicationID);
        Assert.Equal(AppStatus.Completed, result.Value[1].ApplicationStatus);
    }

    [Fact]
    public async Task GetAllApplicationsAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllApplicationsAsync())
            .ReturnsAsync([]);

        var result =
            await service.GetAllApplicationsAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GET BASIC INFO
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBasicInfoAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetBasicInfoAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetBasicInfoAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetApplicationByIdAsync(10))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.GetBasicInfoAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task GetBasicInfoAsync_WhenApplicationExists_ReturnsMappedBasicInfo()
    {
        var service = CreateService();

        var entity =
            CreateApplication(
                10,
                200,
                3,
                AppStatus.New,
                125m);

        entity.Person = new Person
        {
            PersonId = 200,
            FirstName = "Ahmad",
            SecondName = "Mohammed",
            LastName = "Obeidat",
            NationalNo = "9901234567",
            DateOfBirth = DateTime.Today.AddYears(-30),
            Gender = Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            NationalityCountryID = 1
        };

        entity.ApplicationType =
            CreateApplicationType(
                3,
                125m,
                "Renew License");

        entity.CreatedByUser =
            new User
            {
                UserId = 10,
                UserName = "admin"
            };

        _repository
            .Setup(x => x.GetApplicationByIdAsync(10))
            .ReturnsAsync(entity);

        var result =
            await service.GetBasicInfoAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(10, result.Value!.ApplicationID);
        Assert.Equal(200, result.Value.ApplicantPersonID);
        Assert.Equal(
            AppStatus.New,
            result.Value.ApplicationStatus);
        Assert.Equal(125m, result.Value.PaidFees);
        Assert.Equal(
            "Renew License",
            result.Value.ApplicationTypeName);
        Assert.Equal(
            "Ahmad Mohammed Obeidat",
            result.Value.ApplicantFullName);
        Assert.Equal(
            "admin",
            result.Value.CreatedByUserName);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GetApplicationByIdAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetApplicationByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetApplicationByIdAsync(10))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.GetApplicationByIdAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_WhenApplicationExists_ReturnsMappedDto()
    {
        var service = CreateService();

        var entity =
            CreateApplication(
                10,
                200,
                3,
                AppStatus.Completed,
                125m);

        entity.CreatedByUser =
            new User
            {
                UserId = 10,
                UserName = "admin"
            };

        _repository
            .Setup(x => x.GetApplicationByIdAsync(10))
            .ReturnsAsync(entity);

        var result =
            await service.GetApplicationByIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(10, result.Value!.ApplicationID);
        Assert.Equal(200, result.Value.ApplicantPersonID);
        Assert.Equal(3, result.Value.ApplicationTypeID);
        Assert.Equal(
            AppStatus.Completed,
            result.Value.ApplicationStatus);
        Assert.Equal(125m, result.Value.PaidFees);
        Assert.Equal(
            "admin",
            result.Value.CreatedByUserName);
    }

    // =========================================================
    // ADD NEW APPLICATION - VALIDATION
    // =========================================================

    [Fact]
    public async Task AddNewApplicationAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.AddNewApplicationAsync(
                null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Application data is required.",
            result.Error);

        _repository.Verify(
            x => x.AddNewApplicationAsync(
                It.IsAny<ApplicationD>()),
            Times.Never);
    }

    [Fact]
    public async Task AddNewApplicationAsync_WhenApplicantIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            ValidCreateDto(
                applicantPersonId: 0);

        var result =
            await service.AddNewApplicationAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "A valid applicant person is required.",
            result.Error);

        _applicationTypeRepository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddNewApplicationAsync_WhenApplicationTypeIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            ValidCreateDto(
                applicationTypeId: 0);

        var result =
            await service.AddNewApplicationAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "A valid application type is required.",
            result.Error);

        _applicationTypeRepository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    // =========================================================
    // ADD NEW APPLICATION - AUTH
    // =========================================================

    [Fact]
    public async Task AddNewApplicationAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(10);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _applicationTypeRepository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddNewApplicationAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);
    }

    // =========================================================
    // ADD NEW APPLICATION - TYPE
    // =========================================================

    [Fact]
    public async Task AddNewApplicationAsync_WhenApplicationTypeDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync((ApplicationType?)null);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application type not found.",
            result.Error);

        _repository.Verify(
            x => x.AddNewApplicationAsync(
                It.IsAny<ApplicationD>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Never);
    }

    // =========================================================
    // ADD NEW APPLICATION - SUCCESS
    // =========================================================

    [Fact]
    public async Task AddNewApplicationAsync_WhenValid_CreatesApplicationAndReturnsId()
    {
        var service = CreateService();

        SetupAuthenticatedUser(25);

        var applicationType =
            CreateApplicationType(
                1,
                150m,
                "New Application");

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(applicationType);

        ApplicationD? captured = null;

        _repository
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(entity =>
            {
                captured = entity;
                entity.ApplicationID = 700;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto(
                    applicantPersonId: 55,
                    applicationTypeId: 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(700, result.Value);

        Assert.NotNull(captured);
        Assert.Equal(
            55,
            captured!.ApplicantPersonID);
        Assert.Equal(
            1,
            captured.ApplicationTypeID);
        Assert.Equal(
            AppStatus.New,
            captured.ApplicationStatus);
        Assert.Equal(
            150m,
            captured.PaidFees);
        Assert.Equal(
            25,
            captured.CreatedByUserID);

        Assert.NotEqual(
            default,
            captured.ApplicationDate);

        Assert.NotEqual(
            default,
            captured.LastStatusDate);
    }

    [Fact]
    public async Task AddNewApplicationAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(
                CreateApplicationType());

        _repository
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(x =>
                x.ApplicationID = 700)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(0);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create application.",
            result.Error);
    }

    [Fact]
    public async Task AddNewApplicationAsync_WhenGeneratedIdIsInvalid_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(
                CreateApplicationType());

        _repository
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<ApplicationD>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.AddNewApplicationAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create application.",
            result.Error);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [Fact]
    public async Task UpdateApplicationAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateApplicationAsync(
                null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Application data is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationIdIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto(0));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "A valid application ID is required.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationTypeIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto(
                    applicationTypeId: 0));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "A valid application type is required.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationIsCompleted_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Completed));

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Completed applications cannot be modified.",
            result.Error);

        _applicationTypeRepository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationIsCancelled_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Cancelled));

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cancelled applications cannot be modified.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenApplicationTypeDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(CreateApplication());

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync((ApplicationType?)null);

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto(1, 2));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application type not found.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenValid_UpdatesTypeAndFees()
    {
        var service = CreateService();

        var entity =
            CreateApplication(
                1,
                100,
                1,
                AppStatus.New,
                50m);

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync(
                CreateApplicationType(
                    2,
                    200m,
                    "Updated"));

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto(1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            2,
            entity.ApplicationTypeID);
        Assert.Equal(
            200m,
            entity.PaidFees);
    }

    [Fact]
    public async Task UpdateApplicationAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateApplication();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _applicationTypeRepository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync(
                CreateApplicationType(
                    2,
                    200m));

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(0);

        var result =
            await service.UpdateApplicationAsync(
                ValidUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Application update failed.",
            result.Error);
    }

    // =========================================================
    // DELETE
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteApplicationAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.DeleteApplicationAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.DeleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenApplicationIsCompleted_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Completed));

        var result =
            await service.DeleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot delete completed application.",
            result.Error);

        _repository.Verify(
            x => x.DeleteApplication(
                It.IsAny<ApplicationD>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenValid_DeletesAndReturnsSuccess()
    {
        var service = CreateService();

        var entity = CreateApplication();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.DeleteApplicationAsync(1);

        Assert.True(result.IsSuccess);

        _repository.Verify(
            x => x.DeleteApplication(entity),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task DeleteApplicationAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateApplication();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(0);

        var result =
            await service.DeleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Delete application failed.",
            result.Error);
    }

    // =========================================================
    // CANCEL
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CancelApplicationAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.CancelApplicationAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelApplicationAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.CancelApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task CancelApplicationAsync_WhenApplicationIsCompleted_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Completed));

        var result =
            await service.CancelApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot cancel completed application.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task CancelApplicationAsync_WhenAlreadyCancelled_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Cancelled));

        var result =
            await service.CancelApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Application already cancelled.",
            result.Error);
    }

    [Fact]
    public async Task CancelApplicationAsync_WhenValid_ChangesStatusAndSaves()
    {
        var service = CreateService();

        var entity =
            CreateApplication(
                status: AppStatus.New);

        var oldStatusDate =
            entity.LastStatusDate;

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.CancelApplicationAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            AppStatus.Cancelled,
            entity.ApplicationStatus);
        Assert.True(
            entity.LastStatusDate >= oldStatusDate);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CancelApplicationAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateApplication();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(0);

        var result =
            await service.CancelApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Cancel application failed.",
            result.Error);
    }

    // =========================================================
    // COMPLETE
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CompleteApplicationAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.CompleteApplicationAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteApplicationAsync_WhenApplicationDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync((ApplicationD?)null);

        var result =
            await service.CompleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application not found.",
            result.Error);
    }

    [Fact]
    public async Task CompleteApplicationAsync_WhenAlreadyCompleted_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Completed));

        var result =
            await service.CompleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Application already completed.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task CompleteApplicationAsync_WhenCancelled_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(
                CreateApplication(
                    status: AppStatus.Cancelled));

        var result =
            await service.CompleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot complete cancelled application.",
            result.Error);
    }

    [Fact]
    public async Task CompleteApplicationAsync_WhenValid_ChangesStatusAndSaves()
    {
        var service = CreateService();

        var entity =
            CreateApplication(
                status: AppStatus.New);

        var oldStatusDate =
            entity.LastStatusDate;

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(1);

        var result =
            await service.CompleteApplicationAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            AppStatus.Completed,
            entity.ApplicationStatus);
        Assert.True(
            entity.LastStatusDate >= oldStatusDate);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CompleteApplicationAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateApplication();

        _repository
            .Setup(x =>
                x.GetApplicationForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ReturnsAsync(0);

        var result =
            await service.CompleteApplicationAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Complete application failed.",
            result.Error);
    }
}
