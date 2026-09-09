using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class ApplicationTypeServiceTests
{
    private readonly Mock<IApplicationTypeRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ApplicationTypeService CreateService() =>
        new(
            _repository.Object,
            _unitOfWork.Object);

    private static ApplicationType CreateEntity(
        int id = 1,
        string title = "New License",
        decimal fees = 50m)
        => new()
        {
            ApplicationTypeId = id,
            ApplicationTypeTitle = title,
            ApplicationFees = fees
        };

    private static ApplicationTypeDto CreateDto(
        int id = 1,
        string title = "New License",
        decimal fees = 50m)
        => new()
        {
            ApplicationTypeId = id,
            ApplicationTypeTitle = title,
            ApplicationTypeFees = fees
        };

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllApplicationTypesAsync_WhenRepositoryReturnsData_ReturnsMappedDtos()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync(
            [
                CreateEntity(1, "New License", 50m),
                CreateEntity(2, "Renew License", 75m)
            ]);

        var result =
            await service.GetAllApplicationTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(1, result.Value[0].ApplicationTypeId);
        Assert.Equal(
            "New License",
            result.Value[0].ApplicationTypeTitle);
        Assert.Equal(
            50m,
            result.Value[0].ApplicationTypeFees);

        Assert.Equal(2, result.Value[1].ApplicationTypeId);
        Assert.Equal(
            "Renew License",
            result.Value[1].ApplicationTypeTitle);
        Assert.Equal(
            75m,
            result.Value[1].ApplicationTypeFees);
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllApplicationTypesAsync())
            .ReturnsAsync([]);

        var result =
            await service.GetAllApplicationTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetApplicationTypeByIdAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetApplicationTypeByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid application type ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(10))
            .ReturnsAsync((ApplicationType?)null);

        var result =
            await service.GetApplicationTypeByIdAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Application type not found.",
            result.Error);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(10))
            .ReturnsAsync(
                CreateEntity(
                    10,
                    "Renew License",
                    125m));

        var result =
            await service.GetApplicationTypeByIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(
            10,
            result.Value!.ApplicationTypeId);
        Assert.Equal(
            "Renew License",
            result.Value.ApplicationTypeTitle);
        Assert.Equal(
            125m,
            result.Value.ApplicationTypeFees);
    }

    // =========================================================
    // UPDATE - VALIDATION
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateApplicationTypeAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.UpdateApplicationTypeAsync(
                id,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid application type ID.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Application type data is required.",
            result.Error);

        _repository.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenTitleIsEmpty_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            CreateDto(
                title: "   ");

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Application type title is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenTitleExceeds100Characters_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            CreateDto(
                title: new string('x', 101));

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Application type title cannot exceed 100 characters.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenFeesAreNegative_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            CreateDto(
                fees: -1m);

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Application type fees cannot be negative.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenFeesExceedMaximum_ReturnsValidation()
    {
        var service = CreateService();

        var dto =
            CreateDto(
                fees: 9999999999999999.99m + 0.01m);

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Application type fees exceed the allowed value.",
            result.Error);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenMultipleValidationRulesFail_ReturnsAllErrors()
    {
        var service = CreateService();

        var dto =
            CreateDto(
                title: "   ",
                fees: -1m);

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Contains(
            "Application type title is required.",
            result.Error);

        Assert.Contains(
            "Application type fees cannot be negative.",
            result.Error);
    }

    // =========================================================
    // UPDATE - NOT FOUND
    // =========================================================

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenApplicationTypeDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(10))
            .ReturnsAsync((ApplicationType?)null);

        var result =
            await service.UpdateApplicationTypeAsync(
                10,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Application type not found.",
            result.Error);

        _repository.Verify(
            x => x.UpdateApplicationTypeAsync(
                It.IsAny<ApplicationType>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // UPDATE - REPOSITORY FAILURE
    // =========================================================

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenRepositoryUpdateFails_ReturnsFailure()
    {
        var service = CreateService();

        var entity =
            CreateEntity(
                10,
                "Old",
                50m);

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(10))
            .ReturnsAsync(entity);

        _repository
            .Setup(x =>
                x.UpdateApplicationTypeAsync(entity))
            .ReturnsAsync(false);

        var result =
            await service.UpdateApplicationTypeAsync(
                10,
                CreateDto(
                    10,
                    "New",
                    100m));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to update application type.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // UPDATE - SUCCESS
    // =========================================================

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenValid_UpdatesEntityAndSaves()
    {
        var service = CreateService();

        var entity =
            CreateEntity(
                10,
                "Old Title",
                50m);

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(10))
            .ReturnsAsync(entity);

        _repository
            .Setup(x =>
                x.UpdateApplicationTypeAsync(entity))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateApplicationTypeAsync(
                10,
                CreateDto(
                    10,
                    "  Updated Title  ",
                    125m));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Updated Title",
            entity.ApplicationTypeTitle);
        Assert.Equal(
            125m,
            entity.ApplicationFees);

        _repository.Verify(
            x =>
                x.UpdateApplicationTypeAsync(entity),
            Times.Once);

        _unitOfWork.Verify(
            x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenTitleIsExactly100Characters_IsValid()
    {
        var service = CreateService();

        var title =
            new string('x', 100);

        var entity =
            CreateEntity();

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(entity);

        _repository
            .Setup(x =>
                x.UpdateApplicationTypeAsync(entity))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                CreateDto(
                    title: $"  {title}  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            title,
            entity.ApplicationTypeTitle);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        var entity =
            CreateEntity();

        _repository
            .Setup(x =>
                x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(entity);

        _repository
            .Setup(x =>
                x.UpdateApplicationTypeAsync(entity))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.UpdateApplicationTypeAsync(
                1,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to save application type changes.",
            result.Error);
    }
}
