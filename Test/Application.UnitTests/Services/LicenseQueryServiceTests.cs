using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class LicenseQueryServiceTests
{
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localApplicationService = new();
    private readonly Mock<IDetainedLicenseRepository> _detainedRepository = new();

    private LicenseQueryService CreateService() =>
        new(
            _licenseRepository.Object,
            _localApplicationService.Object,
            _detainedRepository.Object);

    private static License CreateLicense(
        int id = 1,
        int driverId = 10,
        int licenseClass = 3) =>
        new()
        {
            LicenseID = id,
            DriverID = driverId,
            LicenseClass = licenseClass,
            IsActive = true,
            IssueDate = new DateTime(2026, 1, 1),
            ExpirationDate = new DateTime(2028, 1, 1)
        };

    [Fact]
    public void Constructor_WhenLicenseRepositoryIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new LicenseQueryService(
                null!,
                _localApplicationService.Object,
                _detainedRepository.Object));
        Assert.Equal("licenseRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenLocalApplicationServiceIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new LicenseQueryService(
                _licenseRepository.Object,
                null!,
                _detainedRepository.Object));
        Assert.Equal("localApplicationService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenDetainedRepositoryIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new LicenseQueryService(
                _licenseRepository.Object,
                _localApplicationService.Object,
                null!));
        Assert.Equal("detainedLicenseRepository", ex.ParamName);
    }

    [Fact]
    public async Task GetById_WhenInvalidId_ReturnsValidationAndDoesNotQuery()
    {
        var result = await CreateService().GetByIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid license ID.", result.Error);
        _licenseRepository.Verify(
            x => x.GetLicenseByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ReturnsAsync((License?)null);

        var result = await CreateService().GetByIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("License not found.", result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsSuccess()
    {
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ReturnsAsync(CreateLicense(5));

        var result = await CreateService().GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.LicenseID);
        Assert.Equal(10, result.Value.DriverID);
    }

    [Fact]
    public async Task GetAll_WhenRepositoryReturnsLicenses_ReturnsMappedList()
    {
        _licenseRepository.Setup(x => x.GetAllLicensesAsync())
            .ReturnsAsync([CreateLicense(1), CreateLicense(2)]);

        var result = await CreateService().GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(1, result.Value[0].LicenseID);
        Assert.Equal(2, result.Value[1].LicenseID);
    }

    [Fact]
    public async Task GetByDriverId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByDriverIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid driver ID.", result.Error);
        _licenseRepository.Verify(
            x => x.GetLicensesByDriverIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByDriverId_WhenValid_ReturnsLicenses()
    {
        _licenseRepository.Setup(x => x.GetLicensesByDriverIdAsync(10))
            .ReturnsAsync([CreateLicense(1, 10), CreateLicense(2, 10)]);

        var result = await CreateService().GetByDriverIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetByApplicationId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByApplicationIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid application ID.", result.Error);
    }

    [Fact]
    public async Task GetByApplicationId_WhenValid_ReturnsLicenses()
    {
        _licenseRepository.Setup(x => x.GetLicensesByApplicationIdAsync(20))
            .ReturnsAsync([CreateLicense(1)]);

        var result = await CreateService().GetByApplicationIdAsync(20);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByLicenseClassIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid license class ID.", result.Error);
    }

    [Fact]
    public async Task GetByLicenseClassId_WhenValid_ReturnsLicenses()
    {
        _licenseRepository.Setup(x => x.GetLicensesByLicenseClassIdAsync(3))
            .ReturnsAsync([CreateLicense(1, 10, 3)]);

        var result = await CreateService().GetByLicenseClassIdAsync(3);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetByPersonId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetLicensesByPersonIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid person ID.", result.Error);
    }

    [Fact]
    public async Task GetByPersonId_WhenValid_ReturnsLicenses()
    {
        _licenseRepository.Setup(x => x.GetLicensesByPersonIdAsync(30))
            .ReturnsAsync([CreateLicense(4)]);

        var result = await CreateService().GetLicensesByPersonIdAsync(30);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task IsLicenseExists_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().IsLicenseExistsAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid license ID.", result.Error);
        _licenseRepository.Verify(
            x => x.IsLicenseExistsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IsLicenseExists_WhenValid_ReturnsRepositoryValue()
    {
        _licenseRepository.Setup(x => x.IsLicenseExistsAsync(10))
            .ReturnsAsync(true);

        var result = await CreateService().IsLicenseExistsAsync(10);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsDriverHasLicense_WhenValid_ReturnsRepositoryValue()
    {
        _licenseRepository.Setup(x => x.IsDriverHasLicenseAsync(10))
            .ReturnsAsync(false);

        var result = await CreateService().IsDriverHasLicenseAsync(10);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsApplicationHasLicense_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().IsApplicationHasLicenseAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid application ID.", result.Error);
    }

    [Fact]
    public async Task IsApplicationHasLicense_WhenValid_ReturnsRepositoryValue()
    {
        _licenseRepository.Setup(x => x.IsApplicationHasLicenseAsync(20))
            .ReturnsAsync(true);

        var result = await CreateService().IsApplicationHasLicenseAsync(20);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetLicenseDetailsByIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid license ID.", result.Error);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenNotFound_ReturnsNotFound()
    {
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ReturnsAsync((License?)null);

        var result = await CreateService().GetLicenseDetailsByIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("License not found.", result.Error);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenDriverIdInvalid_ReturnsFailure()
    {
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ReturnsAsync(CreateLicense(5, 0));

        var result = await CreateService().GetLicenseDetailsByIdAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("License has an invalid driver.", result.Error);
        _detainedRepository.Verify(
            x => x.IsLicenseDetainedAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenDriverNotLoaded_ReturnsFailure()
    {
        var license = CreateLicense(5);
        license.Driver = null;

        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ReturnsAsync(license);

        var result = await CreateService().GetLicenseDetailsByIdAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "License driver information was not loaded.",
            result.Error);
    }

    [Fact]
    public async Task GetDetails_WhenInvalidLocalId_ReturnsValidation()
    {
        var result = await CreateService().GetDetailsAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid local application ID.", result.Error);
    }

    [Fact]
    public async Task GetDetails_WhenLocalApplicationFails_PropagatesFailure()
    {
        _localApplicationService
            .Setup(x => x.GetLocalDrivingLicenseApplicationByIdAsync(5))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>.FromNotFound(
                    "Local application not found."));

        var result = await CreateService().GetDetailsAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Local application not found.", result.Error);
        _licenseRepository.Verify(
            x => x.GetLicensesByApplicationIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDetails_WhenLocalApplicationValueIsNull_ReturnsNotFound()
    {
        _localApplicationService
            .Setup(x => x.GetLocalDrivingLicenseApplicationByIdAsync(5))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>.Success(null!));

        var result = await CreateService().GetDetailsAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Local driving license application not found.",
            result.Error);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenRepositoryThrows_PropagatesException()
    {
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(5))
            .ThrowsAsync(new InvalidOperationException("query failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetLicenseDetailsByIdAsync(5));

        Assert.Equal("query failed", ex.Message);
    }
}
