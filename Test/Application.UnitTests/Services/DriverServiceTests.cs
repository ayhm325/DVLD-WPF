using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public sealed class DriverServiceTests
{
    private readonly Mock<IDriverRepository> _repository = new();
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private DriverService CreateService() =>
        new(
            _repository.Object,
            _personRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object);

    private void SetupAuthenticatedUser(int userId = 10)
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(userId);
    }

    private static CreateDriverDto CreateDto(int personId = 100) =>
        new()
        {
            PersonID = personId
        };

    private static UpdateDriverDto CreateUpdateDto(
        int driverId = 1,
        int personId = 100) =>
        new()
        {
            DriverID = driverId,
            PersonID = personId
        };

    private static Driver CreateDriver(
        int driverId = 1,
        int personId = 100,
        int createdByUserId = 10)
        => new()
        {
            DriverID = driverId,
            PersonID = personId,
            CreatedByUserID = createdByUserId,
            CreatedDate = new DateTime(
                2026, 1, 15,
                10, 30, 0,
                DateTimeKind.Utc),
            Person = new Person
            {
                PersonId = personId,
                FirstName = "Ahmad",
                SecondName = "Mohammed",
                ThirdName = "Ali",
                LastName = "Obeidat",
                NationalNo = "9901234567",
                DateOfBirth = new DateTime(
                    1996, 5, 10),
                Gender = Gender.Male,
                ImagePath = "person.jpg"
            },
            CreatedByUser = new User
            {
                UserId = createdByUserId,
                UserName = "admin"
            }
        };

    // =========================================================
    // GET BY ID
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid driver ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync((Driver?)null);

        var result =
            await service.GetByIdAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Driver not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDriverExists_ReturnsMappedDto()
    {
        var service = CreateService();

        var driver = CreateDriver(
            10,
            200,
            20);

        driver.Licenses.Add(
            new License
            {
                LicenseID = 1,
                DriverID = 10,
                IsActive = true
            });

        driver.Licenses.Add(
            new License
            {
                LicenseID = 2,
                DriverID = 10,
                IsActive = true
            });

        driver.Licenses.Add(
            new License
            {
                LicenseID = 3,
                DriverID = 10,
                IsActive = false
            });

        _repository
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(driver);

        var result =
            await service.GetByIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(10, result.Value!.DriverID);
        Assert.Equal(200, result.Value.PersonID);
        Assert.Equal(
            "Ahmad Mohammed Ali Obeidat",
            result.Value.FullName);
        Assert.Equal(
            "9901234567",
            result.Value.NationalNo);
        Assert.Equal(
            Gender.Male,
            result.Value.Gender);
        Assert.Equal(
            "person.jpg",
            result.Value.ImagePath);
        Assert.Equal(
            20,
            result.Value.CreatedByUserID);
        Assert.Equal(
            "admin",
            result.Value.CreatedByUserName);
        Assert.Equal(
            2,
            result.Value.ActiveLicenses);
        Assert.Equal(
            "2026-01-15",
            result.Value.CreatedDateFormatted);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsDrivers_ReturnsMappedDtos()
    {
        var service = CreateService();

        var first = CreateDriver(1, 100, 10);
        var second = CreateDriver(2, 200, 11);

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
            [
                first,
                second
            ]);

        var result =
            await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(1, result.Value[0].DriverID);
        Assert.Equal(100, result.Value[0].PersonID);

        Assert.Equal(2, result.Value[1].DriverID);
        Assert.Equal(200, result.Value[1].PersonID);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        var result =
            await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPersonIdAsync_WhenPersonIdIsInvalid_ReturnsValidation(
        int personId)
    {
        var service = CreateService();

        var result =
            await service.GetByPersonIdAsync(personId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid person ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByPersonIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByPersonIdAsync_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetByPersonIdAsync(100))
            .ReturnsAsync((Driver?)null);

        var result =
            await service.GetByPersonIdAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Driver not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByPersonIdAsync_WhenDriverExists_ReturnsMappedDto()
    {
        var service = CreateService();

        var driver =
            CreateDriver(
                5,
                100,
                20);

        _repository
            .Setup(x => x.GetByPersonIdAsync(100))
            .ReturnsAsync(driver);

        var result =
            await service.GetByPersonIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.DriverID);
        Assert.Equal(100, result.Value.PersonID);
    }

    // =========================================================
    // GET BY CREATED USER
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByCreatedUserIdAsync_WhenUserIdIsInvalid_ReturnsValidation(
        int userId)
    {
        var service = CreateService();

        var result =
            await service.GetByCreatedUserIdAsync(userId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid creating user ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByCreatedUserIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_WhenValid_ReturnsMappedDtos()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetByCreatedUserIdAsync(10))
            .ReturnsAsync(
            [
                CreateDriver(1, 100, 10),
                CreateDriver(2, 200, 10)
            ]);

        var result =
            await service.GetByCreatedUserIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(
            result.Value,
            x => Assert.Equal(
                10,
                x.CreatedByUserID));
    }

    // =========================================================
    // EXISTS
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExistsByIdAsync_WhenIdIsInvalid_ReturnsFalse(
        int id)
    {
        var service = CreateService();

        var result =
            await service.ExistsByIdAsync(id);

        Assert.False(result);

        _repository.Verify(
            x => x.ExistsByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ExistsByIdAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.ExistsByIdAsync(10))
            .ReturnsAsync(true);

        var result =
            await service.ExistsByIdAsync(10);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByIdAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.ExistsByIdAsync(10))
            .ReturnsAsync(false);

        var result =
            await service.ExistsByIdAsync(10);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExistsByPersonIdAsync_WhenPersonIdIsInvalid_ReturnsFalse(
        int personId)
    {
        var service = CreateService();

        var result =
            await service.ExistsByPersonIdAsync(personId);

        Assert.False(result);

        _repository.Verify(
            x => x.ExistsByPersonIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(true);

        var result =
            await service.ExistsByPersonIdAsync(100);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(false);

        var result =
            await service.ExistsByPersonIdAsync(100);

        Assert.False(result);
    }

    // =========================================================
    // ADD
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.AddAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Driver data is required.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<Driver>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddAsync_WhenPersonIdIsInvalid_ReturnsValidation(
        int personId)
    {
        var service = CreateService();

        var result =
            await service.AddAsync(
                CreateDto(personId));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "A valid person is required.",
            result.Error);

        _personRepository.Verify(
            x => x.IsPersonExistsByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(10);

        var result =
            await service.AddAsync(
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _personRepository.Verify(
            x => x.IsPersonExistsByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var result =
            await service.AddAsync(
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(false);

        var result =
            await service.AddAsync(
                CreateDto(100));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Person not found.",
            result.Error);

        _repository.Verify(
            x => x.ExistsByPersonIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenPersonIsAlreadyDriver_ReturnsConflict()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(true);

        var result =
            await service.AddAsync(
                CreateDto(100));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);
        Assert.Equal(
            "This person is already registered as a driver.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<Driver>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenValid_CreatesDriverWithCurrentUserAndReturnsId()
    {
        var service = CreateService();

        SetupAuthenticatedUser(25);

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(false);

        Driver? captured = null;

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Driver>()))
            .Callback<Driver>(driver =>
            {
                captured = driver;
                driver.DriverID = 50;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.AddAsync(
                CreateDto(100));

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);

        Assert.NotNull(captured);
        Assert.Equal(100, captured!.PersonID);
        Assert.Equal(25, captured.CreatedByUserID);
        Assert.NotEqual(
            default,
            captured.CreatedDate);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<Driver>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Driver>()))
            .Callback<Driver>(x =>
                x.DriverID = 50)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.AddAsync(
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to create driver.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenGeneratedIdIsInvalid_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Driver>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.AddAsync(
                CreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to create driver.",
            result.Error);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenDtoIsNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Driver data is required.",
            result.Error);

        _repository.Verify(
            x => x.GetForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDriverIdIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    driverId: 0));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "A valid driver ID is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenPersonIdIsInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    personId: 0));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "A valid person is required.",
            result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(10);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _repository.Verify(
            x => x.GetForUpdateAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync((Driver?)null);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Driver not found.",
            result.Error);

        _personRepository.Verify(
            x => x.IsPersonExistsByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(
                CreateDriver(1, 100));

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(200))
            .ReturnsAsync(false);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    1,
                    200));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Person not found.",
            result.Error);

        _repository.Verify(
            x => x.ExistsByPersonIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewPersonAlreadyBelongsToAnotherDriver_ReturnsConflict()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(
                CreateDriver(1, 100));

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(200))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(200))
            .ReturnsAsync(true);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    1,
                    200));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);
        Assert.Equal(
            "This person is already registered as another driver.",
            result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSamePersonIsKept_DoesNotCheckDuplicateDriver()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var existing =
            CreateDriver(1, 100);

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(existing);

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    1,
                    100));

        Assert.True(result.IsSuccess);

        _repository.Verify(
            x => x.ExistsByPersonIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_ChangesPersonAndSaves()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var existing =
            CreateDriver(1, 100);

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(existing);

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(200))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(200))
            .ReturnsAsync(false);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    1,
                    200));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            200,
            existing.PersonID);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var existing =
            CreateDriver(1, 100);

        _repository
            .Setup(x => x.GetForUpdateAsync(1))
            .ReturnsAsync(existing);

        _personRepository
            .Setup(x =>
                x.IsPersonExistsByIdAsync(200))
            .ReturnsAsync(true);

        _repository
            .Setup(x =>
                x.ExistsByPersonIdAsync(200))
            .ReturnsAsync(false);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.UpdateAsync(
                CreateUpdateDto(
                    1,
                    200));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "No driver changes were saved.",
            result.Error);
    }

    // =========================================================
    // DELETE
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.DeleteAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid driver ID.",
            result.Error);

        _repository.Verify(
            x => x.GetForDeleteAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(10);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _repository.Verify(
            x => x.GetForDeleteAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        _repository
            .Setup(x => x.GetForDeleteAsync(1))
            .ReturnsAsync((Driver?)null);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Driver not found.",
            result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenDriverHasLicenses_ReturnsConflict()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var driver =
            CreateDriver(1, 100);

        driver.Licenses.Add(
            new License
            {
                LicenseID = 50,
                DriverID = 1
            });

        _repository
            .Setup(x => x.GetForDeleteAsync(1))
            .ReturnsAsync(driver);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);
        Assert.Equal(
            "Cannot delete a driver with existing licenses.",
            result.Error);

        _repository.Verify(
            x => x.Delete(
                It.IsAny<Driver>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenDriverHasInternationalLicense_ReturnsConflict()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var driver =
            CreateDriver(1, 100);

        driver.InternationalLicenses.Add(
            new InternationalLicense
            {
                InternationalLicenseID = 50,
                DriverID = 1
            });

        _repository
            .Setup(x => x.GetForDeleteAsync(1))
            .ReturnsAsync(driver);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);
        Assert.Equal(
            "Cannot delete a driver with an existing international license.",
            result.Error);

        _repository.Verify(
            x => x.Delete(
                It.IsAny<Driver>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryDeleteIsCalled_AndSaveSucceeds_ReturnsSuccess()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var driver =
            CreateDriver(1, 100);

        _repository
            .Setup(x => x.GetForDeleteAsync(1))
            .ReturnsAsync(driver);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsSuccess);

        _repository.Verify(
            x => x.Delete(driver),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSaveReturnsZero_ReturnsFailure()
    {
        var service = CreateService();

        SetupAuthenticatedUser();

        var driver =
            CreateDriver(1, 100);

        _repository
            .Setup(x => x.GetForDeleteAsync(1))
            .ReturnsAsync(driver);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);
        Assert.Equal(
            "Failed to delete driver.",
            result.Error);
    }
}
