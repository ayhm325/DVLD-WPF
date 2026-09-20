using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public sealed class LocalDrivingLicenseApplicationServiceTests
{
    private readonly Mock<ILocalDrivingLicenseApplicationRepository> _repository = new();
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IApplicationRepository> _applicationRepository = new();
    private readonly Mock<IApplicationTypeRepository> _applicationTypeRepository = new();
    private readonly Mock<ILicenseClassService> _licenseClassService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IApplicationService> _applicationService = new();

    private LocalDrivingLicenseApplicationService CreateService() => new(
        _repository.Object, _licenseRepository.Object, _unitOfWork.Object,
        _applicationRepository.Object, _applicationTypeRepository.Object,
        _licenseClassService.Object, _currentUserService.Object,
        _applicationService.Object);

    private static LocalDrivingLicenseApplication CreateEntity(
        int id = 1, int applicationId = 10, int licenseClassId = 3) => new()
        {
            LocalDrivingLicenseApplicationID = id,
            ApplicationID = applicationId,
            LicenseClassID = licenseClassId,
            Application = new ApplicationD
            {
                ApplicationID = applicationId,
                ApplicantPersonID = 20,
                ApplicationTypeID = 1,
                ApplicationStatus = AppStatus.New,
                PaidFees = 25m,
                ApplicationDate = new DateTime(2026, 1, 1)
            }
        };

    [Fact]
    public void Constructor_WhenAnyDependencyIsNull_ThrowsArgumentNullException()
    {
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                null!, _licenseRepository.Object, _unitOfWork.Object,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);

        Assert.Equal("licenseRepository", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                _repository.Object, null!, _unitOfWork.Object,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);

        Assert.Equal("unitOfWork", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                _repository.Object, _licenseRepository.Object, null!,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);
    }

    [Fact]
    public async Task GetAll_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            CreateService().GetAllLocalDrivingLicenseApplicationsAsync(null!));

        _repository.Verify(x => x.GetAllAsync(It.IsAny<PaginationRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsPagedResult()
    {
        var entities = new[] { CreateEntity(1, 10, 3), CreateEntity(2, 20, 4) };

        _repository.Setup(x => x.GetAllAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<LocalDrivingLicenseApplication>
            {
                Items = entities,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 25
            });

        _repository.Setup(x => x.GetPassedTestCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 2, [2] = 3 });

        _licenseRepository.Setup(x => x.GetApplicationIdsWithLicensesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([10]);

        var result = await CreateService().GetAllLocalDrivingLicenseApplicationsAsync(
            new PaginationRequest { PageNumber = 2, PageSize = 10 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var page = result.Value!;
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.Items.Count);

        _repository.Verify(x => x.GetAllAsync(
            It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 10)), Times.Once);

        _repository.Verify(x => x.GetPassedTestCountsAsync(
            It.Is<IEnumerable<int>>(ids => ids.Contains(1) && ids.Contains(2))), Times.Once);

        _licenseRepository.Verify(x => x.GetApplicationIdsWithLicensesAsync(
            It.Is<IEnumerable<int>>(ids => ids.Contains(10) && ids.Contains(20))), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetLocalDrivingLicenseApplicationByIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid local driving license application ID.", result.Error);
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().GetLocalDrivingLicenseApplicationByIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetNewFees_WhenApplicationTypeNotFound_ReturnsNotFound()
    {
        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync((ApplicationType?)null);

        var result = await CreateService().GetNewLocalDrivingLicenseApplicationFeesAsync();

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Application type not found.", result.Error);
    }

    [Fact]
    public async Task GetNewFees_WhenApplicationTypeExists_ReturnsFees()
    {
        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 42.5m });

        var result = await CreateService().GetNewLocalDrivingLicenseApplicationFeesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(42.5m, result.Value);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetApplicationIdByLocalIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _repository.Verify(x => x.GetApplicationIdByLocalIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetApplicationIdByLocalIdAsync(5)).ReturnsAsync((int?)null);

        var result = await CreateService().GetApplicationIdByLocalIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Main application not found for this local application.", result.Error);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenFound_ReturnsId()
    {
        _repository.Setup(x => x.GetApplicationIdByLocalIdAsync(5)).ReturnsAsync(25);

        var result = await CreateService().GetApplicationIdByLocalIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public async Task Exists_WhenIdInvalid_ReturnsFalse()
    {
        Assert.False(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(0));
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Exists_WhenEntityExists_ReturnsTrue()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));

        Assert.True(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(5));
    }

    [Fact]
    public async Task Exists_WhenEntityMissing_ReturnsFalse()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        Assert.False(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(5));
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetApplicationBasicInfoAsync(0);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenLocalApplicationMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().GetApplicationBasicInfoAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenApplicationNavigationMissing_ReturnsNotFound()
    {
        var entity = CreateEntity(5);
        entity.Application = null!;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().GetApplicationBasicInfoAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Main application information not found.", result.Error);
    }

    [Fact]
    public async Task Cancel_WhenInvalidId_ReturnsValidation()
    {
        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _applicationService.Verify(
            x => x.CancelApplicationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenApplicationMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_WhenNavigationMissing_ReturnsFailure()
    {
        var entity = CreateEntity(5);
        entity.Application = null!;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Main application information is missing.", result.Error);
    }

    [Fact]
    public async Task Cancel_WhenValid_DelegatesToApplicationService()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        var expected = Result.Success();
        _applicationService.Setup(x => x.CancelApplicationAsync(10)).ReturnsAsync(expected);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.Same(expected, result);
        _applicationService.Verify(x => x.CancelApplicationAsync(10), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenInvalidId_ReturnsValidation()
    {
        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(0);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Delete_WhenApplicationStatusIsNotNew_ReturnsConflict()
    {
        var entity = CreateEntity(5);
        entity.Application!.ApplicationStatus = AppStatus.Cancelled;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Only a New application can be deleted.", result.Error);
        _repository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenRepositoryDeleteFails_ReturnsFailure()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(false);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Failed to delete local driving license application.", result.Error);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenDeleteAndSaveSucceed_ReturnsSuccess()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(true);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.True(result.IsSuccess);
        _repository.Verify(x => x.DeleteAsync(5), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenSaveReturnsZero_ReturnsFailure()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(true);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save local driving license application deletion.",
            result.Error);
    }
}