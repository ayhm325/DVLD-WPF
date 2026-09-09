using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class TestTypeServiceTests
{
    private readonly Mock<ITestTypeRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private TestTypeService CreateService() =>
        new(
            _repository.Object,
            _unitOfWork.Object);

    private static TestType CreateEntity(
        int id = 1,
        string title = "Theory Test",
        string description = "Written theory examination",
        decimal fees = 20m)
        => new()
        {
            TestTypeId = id,
            TestTypeTitle = title,
            TestTypeDescription = description,
            TestTypeFees = fees
        };

    private static TestTypeDto CreateDto(
        int id = 1,
        string title = "Theory Test",
        string description = "Written theory examination",
        decimal fees = 20m)
        => new()
        {
            TestTypeId = id,
            TestTypeTitle = title,
            TestTypeDescription = description,
            TestTypeFees = fees
        };

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllTestTypesAsync_WhenRepositoryReturnsData_ReturnsMappedDtos()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
            [
                CreateEntity(
                    1,
                    "Theory Test",
                    "Written theory examination",
                    20m),
                CreateEntity(
                    2,
                    "Practical Test",
                    "Practical driving examination",
                    35m)
            ]);

        var result =
            await service.GetAllTestTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(
            1,
            result.Value[0].TestTypeId);
        Assert.Equal(
            "Theory Test",
            result.Value[0].TestTypeTitle);
        Assert.Equal(
            "Written theory examination",
            result.Value[0].TestTypeDescription);
        Assert.Equal(
            20m,
            result.Value[0].TestTypeFees);

        Assert.Equal(
            2,
            result.Value[1].TestTypeId);
        Assert.Equal(
            "Practical Test",
            result.Value[1].TestTypeTitle);
    }

    [Fact]
    public async Task GetAllTestTypesAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        var result =
            await service.GetAllTestTypesAsync();

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
    [InlineData(-100)]
    public async Task GetTestTypeByIdAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetTestTypeByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid test type ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTestTypeByIdAsync_WhenTestTypeDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync((TestType?)null);

        var result =
            await service.GetTestTypeByIdAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Test type not found.",
            result.Error);
    }

    [Fact]
    public async Task GetTestTypeByIdAsync_WhenTestTypeExists_ReturnsMappedDto()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(
                CreateEntity(
                    10,
                    "Practical Test",
                    "Practical driving examination",
                    35m));

        var result =
            await service.GetTestTypeByIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(
            10,
            result.Value!.TestTypeId);
        Assert.Equal(
            "Practical Test",
            result.Value.TestTypeTitle);
        Assert.Equal(
            "Practical driving examination",
            result.Value.TestTypeDescription);
        Assert.Equal(
            35m,
            result.Value.TestTypeFees);
    }

    // =========================================================
    // UPDATE - VALIDATION
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateTestTypeAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                id,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid test type ID.",
            result.Error);

        _repository.Verify(
            x => x.GetForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type data is required.",
            result.Error);

        _repository.Verify(
            x => x.GetForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenTitleIsWhitespace_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    title: "   "));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type title is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenDescriptionIsWhitespace_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    description: "   "));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type description is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenTitleExceeds100Characters_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    title: new string('x', 101)));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type title cannot exceed 100 characters.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenDescriptionExceeds250Characters_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    description: new string('x', 251)));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type description cannot exceed 250 characters.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenFeesAreNegative_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    fees: -0.01m));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Test type fees cannot be negative.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenMultipleValidationRulesFail_ReturnsAllErrors()
    {
        var service = CreateService();

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    title: "   ",
                    description: "   ",
                    fees: -1m));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Contains(
            "Test type title is required.",
            result.Error);

        Assert.Contains(
            "Test type description is required.",
            result.Error);

        Assert.Contains(
            "Test type fees cannot be negative.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenTitleIsExactly100Characters_IsValid()
    {
        var service = CreateService();

        var entity = CreateEntity();

        var title = new string('T', 100);

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    title: $"  {title}  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            title,
            entity.TestTypeTitle);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenDescriptionIsExactly250Characters_IsValid()
    {
        var service = CreateService();

        var entity = CreateEntity();

        var description =
            new string('D', 250);

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    description: $"  {description}  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            description,
            entity.TestTypeDescription);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenFeesAreZero_IsValid()
    {
        var service = CreateService();

        var entity = CreateEntity();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    fees: 0m));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            0m,
            entity.TestTypeFees);
    }

    // =========================================================
    // UPDATE - NOT FOUND
    // =========================================================

    [Fact]
    public async Task UpdateTestTypeAsync_WhenTestTypeDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetForUpdateAsync(10))
            .ReturnsAsync((TestType?)null);

        var result =
            await service.UpdateTestTypeAsync(
                10,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Test type not found.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // UPDATE - SUCCESS / MAPPING
    // =========================================================

    [Fact]
    public async Task UpdateTestTypeAsync_WhenValid_UpdatesAllFieldsAndSaves()
    {
        var service = CreateService();

        var entity =
            CreateEntity(
                10,
                "Old Title",
                "Old Description",
                20m);

        _repository
            .Setup(x => x.GetForUpdateAsync(10))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateTestTypeAsync(
                10,
                CreateDto(
                    10,
                    "  Updated Title  ",
                    "  Updated Description  ",
                    45m));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            "Updated Title",
            entity.TestTypeTitle);
        Assert.Equal(
            "Updated Description",
            entity.TestTypeDescription);
        Assert.Equal(
            45m,
            entity.TestTypeFees);

        _repository.Verify(
            x => x.GetForUpdateAsync(10),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_TrimsTitleAndDescriptionBeforeSaving()
    {
        var service = CreateService();

        var entity = CreateEntity();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto(
                    title: "   New Title   ",
                    description: "   New Description   "));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "New Title",
            entity.TestTypeTitle);
        Assert.Equal(
            "New Description",
            entity.TestTypeDescription);
    }

    // =========================================================
    // UPDATE - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task UpdateTestTypeAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateEntity();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to update test type.",
            result.Error);
    }

    [Fact]
    public async Task UpdateTestTypeAsync_WhenSaveReturnsNegative_ReturnsFailure()
    {
        var service = CreateService();

        var entity = CreateEntity();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(entity);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1);

        var result =
            await service.UpdateTestTypeAsync(
                1,
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to update test type.",
            result.Error);
    }

    // =========================================================
    // UPDATE - EXCEPTION PROPAGATION
    // =========================================================

    [Fact]
    public async Task UpdateTestTypeAsync_WhenRepositoryThrows_PropagatesException()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    service.UpdateTestTypeAsync(
                        1,
                        CreateDto()));

        Assert.Equal(
            "Database error",
            exception.Message);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
