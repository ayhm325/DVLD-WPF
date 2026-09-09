using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PersonService _sut;

    public PersonServiceTests()
    {
        _personRepositoryMock = new Mock<IPersonRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _sut = new PersonService(
            _personRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // =========================================================
    // GetPersonByIdAsync
    // =========================================================

    [Fact]
    public async Task GetPersonByIdAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidPersonId = 0;

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByIdAsync(invalidPersonId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid person ID.", result.Error);

        _personRepositoryMock.Verify(
            repository => repository.GetPersonByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPersonByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        // Arrange
        const int personId = 10;

        _personRepositoryMock
            .Setup(repository => repository.GetPersonByIdAsync(personId))
            .ReturnsAsync((Person?)null);

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByIdAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Person not found.", result.Error);

        _personRepositoryMock.Verify(
            repository => repository.GetPersonByIdAsync(personId),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonByIdAsync_PersonExists_ReturnsSuccess()
    {
        // Arrange
        const int personId = 10;

        var person = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository => repository.GetPersonByIdAsync(personId))
            .ReturnsAsync(person);

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByIdAsync(personId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);

        Assert.Equal(person.PersonId, result.Value!.PersonId);
        Assert.Equal(person.NationalNo, result.Value.NationalNo);
        Assert.Equal(person.FirstName, result.Value.FirstName);
        Assert.Equal(person.SecondName, result.Value.SecondName);
        Assert.Equal(person.LastName, result.Value.LastName);
        Assert.Equal(person.DateOfBirth, result.Value.DateOfBirth);
        Assert.Equal(person.Gender, result.Value.Gender);
        Assert.Equal(person.Address, result.Value.Address);
        Assert.Equal(person.Phone, result.Value.Phone);
        Assert.Equal(person.Email, result.Value.Email);
        Assert.Equal(
            person.NationalityCountryID,
            result.Value.NationalityCountryID);

        _personRepositoryMock.Verify(
            repository => repository.GetPersonByIdAsync(personId),
            Times.Once);
    }

    // =========================================================
    // GetPersonByNationalNoAsync
    // =========================================================

    [Fact]
    public async Task GetPersonByNationalNoAsync_EmptyNationalNo_ReturnsValidationFailure()
    {
        // Arrange
        const string nationalNo = "   ";

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByNationalNoAsync(nationalNo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "National number is required.",
            result.Error);

        _personRepositoryMock.Verify(
            repository => repository.GetPersonByNationalNoAsync(
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_InvalidFormat_ReturnsValidationFailure()
    {
        // Arrange
        const string nationalNo = "12345";

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByNationalNoAsync(nationalNo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "National number must be exactly 10 digits.",
            result.Error);

        _personRepositoryMock.Verify(
            repository => repository.GetPersonByNationalNoAsync(
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_PersonNotFound_ReturnsNotFound()
    {
        // Arrange
        const string nationalNo = "1234567890";

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync((Person?)null);

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByNationalNoAsync(nationalNo);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Person not found.", result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonByNationalNoAsync(nationalNo),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_ValidNationalNo_ReturnsSuccess()
    {
        // Arrange
        const string nationalNo = "1234567890";

        var person = CreatePerson(
            personId: 10,
            nationalNo: nationalNo);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync(person);

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByNationalNoAsync(nationalNo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);

        Assert.Equal(person.PersonId, result.Value!.PersonId);
        Assert.Equal(nationalNo, result.Value.NationalNo);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonByNationalNoAsync(nationalNo),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_NationalNoHasWhitespace_TrimsBeforeRepositoryCall()
    {
        // Arrange
        const string inputNationalNo = " 1234567890 ";
        const string normalizedNationalNo = "1234567890";

        var person = CreatePerson(
            personId: 10,
            nationalNo: normalizedNationalNo);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonByNationalNoAsync(normalizedNationalNo))
            .ReturnsAsync(person);

        // Act
        Result<PersonDto> result =
            await _sut.GetPersonByNationalNoAsync(inputNationalNo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(
            normalizedNationalNo,
            result.Value!.NationalNo);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonByNationalNoAsync(
                    normalizedNationalNo),
            Times.Once);
    }

    // =========================================================
    // IsPersonExistsAsync
    // =========================================================

    [Fact]
    public async Task IsPersonExistsAsync_InvalidId_ReturnsFalseWithoutRepositoryCall()
    {
        // Arrange
        const int invalidPersonId = 0;

        // Act
        bool result =
            await _sut.IsPersonExistsAsync(invalidPersonId);

        // Assert
        Assert.False(result);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsPersonExistsByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IsPersonExistsAsync_ValidId_DelegatesToRepository()
    {
        // Arrange
        const int personId = 10;

        _personRepositoryMock
            .Setup(repository =>
                repository.IsPersonExistsByIdAsync(personId))
            .ReturnsAsync(true);

        // Act
        bool result =
            await _sut.IsPersonExistsAsync(personId);

        // Assert
        Assert.True(result);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsPersonExistsByIdAsync(personId),
            Times.Once);
    }

    // =========================================================
    // GetAllPeopleAsync
    // =========================================================

    [Fact]
    public async Task GetAllPeopleAsync_PeopleExist_ReturnsMappedPeople()
    {
        // Arrange
        var people = new List<Person>
        {
            CreatePerson(1, "1234567890"),
            CreatePerson(2, "0987654321")
        };

        _personRepositoryMock
            .Setup(repository => repository.GetAllPersonsAsync())
            .ReturnsAsync(people);

        // Act
        Result<List<PersonDto>> result =
            await _sut.GetAllPeopleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);

        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(
            people[0].PersonId,
            result.Value[0].PersonId);

        Assert.Equal(
            people[0].NationalNo,
            result.Value[0].NationalNo);

        Assert.Equal(
            people[1].PersonId,
            result.Value[1].PersonId);

        Assert.Equal(
            people[1].NationalNo,
            result.Value[1].NationalNo);

        _personRepositoryMock.Verify(
            repository => repository.GetAllPersonsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllPeopleAsync_NoPeople_ReturnsEmptyList()
    {
        // Arrange
        _personRepositoryMock
            .Setup(repository => repository.GetAllPersonsAsync())
            .ReturnsAsync(new List<Person>());

        // Act
        Result<List<PersonDto>> result =
            await _sut.GetAllPeopleAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);

        _personRepositoryMock.Verify(
            repository => repository.GetAllPersonsAsync(),
            Times.Once);
    }

    // =========================================================
    // AddPersonAsync
    // =========================================================

    [Fact]
    public async Task AddPersonAsync_InvalidDto_ReturnsValidationFailure()
    {
        // Arrange
        var dto = new PersonCreateDto
        {
            NationalNo = "123",
            FirstName = "",
            SecondName = "",
            LastName = "",
            DateOfBirth = DateTime.Now.AddYears(1),
            Gender = Gender.Male,
            Address = "",
            Phone = "",
            Email = null,
            NationalityCountryID = 0
        };

        // Act
        Result<int> result =
            await _sut.AddPersonAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsNationalNoDuplicatedAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);

        _personRepositoryMock.Verify(
            repository => repository.AddPersonAsync(
                It.IsAny<Person>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPersonAsync_DuplicateNationalNumber_ReturnsConflict()
    {
        // Arrange
        var dto = CreateValidCreateDto();

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    0))
            .ReturnsAsync(true);

        // Act
        Result<int> result =
            await _sut.AddPersonAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The national number is already registered.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    0),
            Times.Once);

        _personRepositoryMock.Verify(
            repository =>
                repository.AddPersonAsync(
                    It.IsAny<Person>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPersonAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        var dto = CreateValidCreateDto();

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    0))
            .ReturnsAsync(false);

        _personRepositoryMock
            .Setup(repository =>
                repository.AddPersonAsync(
                    It.IsAny<Person>()))
            .Callback<Person>(person =>
                person.PersonId = 1)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result<int> result =
            await _sut.AddPersonAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create person.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.AddPersonAsync(
                    It.IsAny<Person>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonAsync_ValidDto_ReturnsCreatedPersonId()
    {
        // Arrange
        var dto = CreateValidCreateDto();

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    0))
            .ReturnsAsync(false);

        _personRepositoryMock
            .Setup(repository =>
                repository.AddPersonAsync(
                    It.IsAny<Person>()))
            .Callback<Person>(person =>
                person.PersonId = 25)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<int> result =
            await _sut.AddPersonAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(25, result.Value);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    0),
            Times.Once);

        _personRepositoryMock.Verify(
            repository =>
                repository.AddPersonAsync(
                    It.Is<Person>(person =>
                        person.NationalNo == dto.NationalNo &&
                        person.FirstName == dto.FirstName &&
                        person.LastName == dto.LastName)),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // UpdatePersonAsync
    // =========================================================

    [Fact]
    public async Task UpdatePersonAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidPersonId = 0;
        var dto = CreateValidUpdateDto();

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                invalidPersonId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid person ID.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdatePersonAsync_InvalidDto_ReturnsValidationFailure()
    {
        // Arrange
        const int personId = 10;

        var dto = new PersonUpdateDto
        {
            NationalNo = "123",
            FirstName = "",
            SecondName = "",
            LastName = "",
            DateOfBirth = DateTime.Now.AddYears(1),
            Gender = Gender.Male,
            Address = "",
            Phone = "",
            Email = null,
            NationalityCountryID = 0
        };

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                personId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdatePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        // Arrange
        const int personId = 10;
        var dto = CreateValidUpdateDto();

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync((Person?)null);

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                personId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Person not found.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(personId),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePersonAsync_DuplicateNationalNumber_ReturnsConflict()
    {
        // Arrange
        const int personId = 10;
        var dto = CreateValidUpdateDto();

        var existingPerson = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(existingPerson);

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    personId))
            .ReturnsAsync(true);

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                personId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The national number is already registered to another person.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    personId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdatePersonAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        const int personId = 10;
        var dto = CreateValidUpdateDto();

        var existingPerson = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(existingPerson);

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    personId))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                personId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "No changes were saved.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePersonAsync_ValidDto_ReturnsSuccess()
    {
        // Arrange
        const int personId = 10;
        var dto = CreateValidUpdateDto();

        var existingPerson = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(existingPerson);

        _personRepositoryMock
            .Setup(repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    personId))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result =
            await _sut.UpdatePersonAsync(
                personId,
                dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        Assert.Equal(dto.NationalNo, existingPerson.NationalNo);
        Assert.Equal(dto.FirstName, existingPerson.FirstName);
        Assert.Equal(dto.SecondName, existingPerson.SecondName);
        Assert.Equal(dto.LastName, existingPerson.LastName);
        Assert.Equal(dto.Phone, existingPerson.Phone);
        Assert.Equal(dto.Email, existingPerson.Email);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(personId),
            Times.Once);

        _personRepositoryMock.Verify(
            repository =>
                repository.IsNationalNoDuplicatedAsync(
                    dto.NationalNo,
                    personId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // DeletePersonAsync
    // =========================================================

    [Fact]
    public async Task DeletePersonAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidPersonId = 0;

        // Act
        Result result =
            await _sut.DeletePersonAsync(invalidPersonId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid person ID.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeletePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        // Arrange
        const int personId = 10;

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync((Person?)null);

        // Act
        Result result =
            await _sut.DeletePersonAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Person not found.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(personId),
            Times.Once);
    }

    [Fact]
    public async Task DeletePersonAsync_HasApplications_ReturnsConflict()
    {
        // Arrange
        const int personId = 10;

        var person = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(person);

        _personRepositoryMock
            .Setup(repository =>
                repository.HasApplicationsAsync(personId))
            .ReturnsAsync(true);

        // Act
        Result result =
            await _sut.DeletePersonAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot delete this person because they have one or more applications.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.DeletePersonAsync(
                    It.IsAny<int>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeletePersonAsync_DeleteFails_ReturnsFailure()
    {
        // Arrange
        const int personId = 10;

        var person = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(person);

        _personRepositoryMock
            .Setup(repository =>
                repository.HasApplicationsAsync(personId))
            .ReturnsAsync(false);

        _personRepositoryMock
            .Setup(repository =>
                repository.DeletePersonAsync(personId))
            .ReturnsAsync(false);

        // Act
        Result result =
            await _sut.DeletePersonAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to delete person.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeletePersonAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        const int personId = 10;

        var person = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(person);

        _personRepositoryMock
            .Setup(repository =>
                repository.HasApplicationsAsync(personId))
            .ReturnsAsync(false);

        _personRepositoryMock
            .Setup(repository =>
                repository.DeletePersonAsync(personId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result result =
            await _sut.DeletePersonAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save person deletion.",
            result.Error);

        _personRepositoryMock.Verify(
            repository =>
                repository.DeletePersonAsync(personId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletePersonAsync_ValidPerson_ReturnsSuccess()
    {
        // Arrange
        const int personId = 10;

        var person = CreatePerson(personId);

        _personRepositoryMock
            .Setup(repository =>
                repository.GetPersonForUpdateAsync(personId))
            .ReturnsAsync(person);

        _personRepositoryMock
            .Setup(repository =>
                repository.HasApplicationsAsync(personId))
            .ReturnsAsync(false);

        _personRepositoryMock
            .Setup(repository =>
                repository.DeletePersonAsync(personId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result =
            await _sut.DeletePersonAsync(personId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        _personRepositoryMock.Verify(
            repository =>
                repository.GetPersonForUpdateAsync(personId),
            Times.Once);

        _personRepositoryMock.Verify(
            repository =>
                repository.HasApplicationsAsync(personId),
            Times.Once);

        _personRepositoryMock.Verify(
            repository =>
                repository.DeletePersonAsync(personId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // Test Data Helpers
    // =========================================================

    private static Person CreatePerson(
        int personId = 10,
        string nationalNo = "1234567890")
    {
        return new Person
        {
            PersonId = personId,
            NationalNo = nationalNo,
            FirstName = "Ayhm",
            SecondName = "Mohammed",
            ThirdName = null,
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1993, 1, 1),
            Gender = Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ayhm@example.com",
            NationalityCountryID = 1,
            ImagePath = null
        };
    }

    private static PersonCreateDto CreateValidCreateDto()
    {
        return new PersonCreateDto
        {
            NationalNo = "1234567890",
            FirstName = "Ayhm",
            SecondName = "Mohammed",
            ThirdName = null,
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1993, 1, 1),
            Gender = Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ayhm@example.com",
            NationalityCountryID = 1,
            ImagePath = null
        };
    }

    private static PersonUpdateDto CreateValidUpdateDto()
    {
        return new PersonUpdateDto
        {
            NationalNo = "9876543210",
            FirstName = "Updated",
            SecondName = "Person",
            ThirdName = "Test",
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1992, 5, 10),
            Gender = Gender.Male,
            Address = "Irbid",
            Phone = "0781234567",
            Email = "updated@example.com",
            NationalityCountryID = 1,
            ImagePath = null
        };
    }
}