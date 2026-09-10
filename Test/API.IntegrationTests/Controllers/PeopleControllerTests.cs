using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Domain.Enums;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class PeopleControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync("api/People");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        factory.PersonServiceMock.Verify(
            x => x.GetAllPeopleAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticated_ReturnsOkWithMappedPeople()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var people = new List<PersonDto>
        {
            CreatePersonDto(
                id: 1,
                nationalNo: "9901234567",
                firstName: "Ahmad",
                secondName: "Mohammed",
                thirdName: "Ali",
                lastName: "Obeidat",
                fullName: "Ahmad Mohammed Ali Obeidat",
                gender: Gender.Male,
                countryName: "Jordan"),

            CreatePersonDto(
                id: 2,
                nationalNo: "9901234568",
                firstName: "Sara",
                secondName: "Mohammed",
                thirdName: null,
                lastName: "Ali",
                fullName: "Sara Mohammed Ali",
                gender: Gender.Female,
                countryName: "Jordan")
        };

        factory.PersonServiceMock
            .Setup(x => x.GetAllPeopleAsync())
            .ReturnsAsync(
                Result<List<PersonDto>>.Success(people));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync("api/People");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        var root =
            document.RootElement;

        Assert.Equal(
            JsonValueKind.Array,
            root.ValueKind);

        Assert.Equal(
            2,
            root.GetArrayLength());

        var firstPerson =
            root[0];

        Assert.Equal(
            1,
            firstPerson.GetProperty("personId").GetInt32());

        Assert.Equal(
            "9901234567",
            firstPerson.GetProperty("nationalNo").GetString());

        Assert.Equal(
            "Ahmad",
            firstPerson.GetProperty("firstName").GetString());

        Assert.Equal(
            "Mohammed",
            firstPerson.GetProperty("secondName").GetString());

        Assert.Equal(
            "Ali",
            firstPerson.GetProperty("thirdName").GetString());

        Assert.Equal(
            "Obeidat",
            firstPerson.GetProperty("lastName").GetString());

        Assert.Equal(
            "Ahmad Mohammed Ali Obeidat",
            firstPerson.GetProperty("fullName").GetString());

        Assert.Equal(
            "Amman",
            firstPerson.GetProperty("address").GetString());

        Assert.Equal(
            "0791234567",
            firstPerson.GetProperty("phone").GetString());

        Assert.Equal(
            "ahmad@example.com",
            firstPerson.GetProperty("email").GetString());

        Assert.Equal(
            1,
            firstPerson.GetProperty("nationalityCountryID").GetInt32());

        Assert.Equal(
            "Jordan",
            firstPerson.GetProperty("countryName").GetString());

        Assert.Equal(
            "ahmad.jpg",
            firstPerson.GetProperty("imagePath").GetString());

        factory.PersonServiceMock.Verify(
            x => x.GetAllPeopleAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsFailure_ReturnsInternalServerError()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Unexpected person retrieval failure.";

        factory.PersonServiceMock
            .Setup(x => x.GetAllPeopleAsync())
            .ReturnsAsync(
                Result<List<PersonDto>>.FromFailure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync("api/People");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task GetById_WhenPersonExists_ReturnsOkWithMappedPerson()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var person =
            CreatePersonDto(
                id: 25,
                nationalNo: "9901234599",
                firstName: "Ahmad",
                secondName: "Mohammed",
                thirdName: "Ali",
                lastName: "Obeidat",
                fullName: "Ahmad Mohammed Ali Obeidat",
                gender: Gender.Male,
                countryName: "Jordan");

        factory.PersonServiceMock
            .Setup(x =>
                x.GetPersonByIdAsync(25))
            .ReturnsAsync(
                Result<PersonDto>.Success(person));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync("api/People/25");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        var body =
            document.RootElement;

        Assert.Equal(
            25,
            body.GetProperty("personId").GetInt32());

        Assert.Equal(
            "9901234599",
            body.GetProperty("nationalNo").GetString());

        Assert.Equal(
            "Ahmad Mohammed Ali Obeidat",
            body.GetProperty("fullName").GetString());

        Assert.Equal(
            "Jordan",
            body.GetProperty("countryName").GetString());

        factory.PersonServiceMock.Verify(
            x => x.GetPersonByIdAsync(25),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Person not found.";

        factory.PersonServiceMock
            .Setup(x =>
                x.GetPersonByIdAsync(999))
            .ReturnsAsync(
                Result<PersonDto>.FromNotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync("api/People/999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);

        factory.PersonServiceMock.Verify(
            x => x.GetPersonByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task GetByNationalNo_WhenPersonExists_ReturnsOkWithMappedPerson()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string nationalNo =
            "9901234567";

        var person =
            CreatePersonDto(
                id: 30,
                nationalNo: nationalNo,
                firstName: "Khaled",
                secondName: "Ali",
                thirdName: null,
                lastName: "Omar",
                fullName: "Khaled Ali Omar",
                gender: Gender.Male,
                countryName: "Jordan");

        factory.PersonServiceMock
            .Setup(x =>
                x.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync(
                Result<PersonDto>.Success(person));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync(
                $"api/People/national/{nationalNo}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        var body =
            document.RootElement;

        Assert.Equal(
            30,
            body.GetProperty("personId").GetInt32());

        Assert.Equal(
            nationalNo,
            body.GetProperty("nationalNo").GetString());

        Assert.Equal(
            "Khaled Ali Omar",
            body.GetProperty("fullName").GetString());

        factory.PersonServiceMock.Verify(
            x =>
                x.GetPersonByNationalNoAsync(nationalNo),
            Times.Once);
    }

    [Fact]
    public async Task GetByNationalNo_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string nationalNo =
            "9999999999";

        const string error =
            "Person not found.";

        factory.PersonServiceMock
            .Setup(x =>
                x.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync(
                Result<PersonDto>.FromNotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.GetAsync(
                $"api/People/national/{nationalNo}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedWithPersonId()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x =>
                x.AddPersonAsync(
                    It.IsAny<PersonCreateDto>()))
            .ReturnsAsync(
                Result<int>.Success(77));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var request = new
        {
            NationalNo = "9901234577",
            FirstName = "Ahmad",
            SecondName = "Mohammed",
            ThirdName = "Ali",
            LastName = "Obeidat",
            DateOfBirth = new DateTime(
                1996,
                5,
                10),
            Gender = (int)Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ahmad@example.com",
            NationalityCountryID = 1,
            ImagePath = "ahmad.jpg"
        };

        var response =
            await client.PostAsJsonAsync(
                "api/People",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        var body =
            document.RootElement;

        Assert.Equal(
            77,
            body.GetProperty("personId").GetInt32());

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            "/api/People/77",
            response.Headers.Location!.ToString());

        factory.PersonServiceMock.Verify(
            x =>
                x.AddPersonAsync(
                    It.Is<PersonCreateDto>(
                        dto =>
                            dto.NationalNo == "9901234577" &&
                            dto.FirstName == "Ahmad" &&
                            dto.SecondName == "Mohammed" &&
                            dto.ThirdName == "Ali" &&
                            dto.LastName == "Obeidat" &&
                            dto.DateOfBirth ==
                                new DateTime(1996, 5, 10) &&
                            dto.Gender == Gender.Male &&
                            dto.Address == "Amman" &&
                            dto.Phone == "0791234567" &&
                            dto.Email == "ahmad@example.com" &&
                            dto.NationalityCountryID == 1 &&
                            dto.ImagePath == "ahmad.jpg")),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "National number is required.";

        factory.PersonServiceMock
            .Setup(x =>
                x.AddPersonAsync(
                    It.IsAny<PersonCreateDto>()))
            .ReturnsAsync(
                Result<int>.FromValidationFailure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.PostAsJsonAsync(
                "api/People",
                new
                {
                    NationalNo = "",
                    FirstName = "Ahmad",
                    SecondName = "Mohammed",
                    ThirdName = (string?)null,
                    LastName = "Obeidat",
                    DateOfBirth = new DateTime(
                        1996,
                        5,
                        10),
                    Gender = (int)Gender.Male,
                    Address = "Amman",
                    Phone = "0791234567",
                    Email = "ahmad@example.com",
                    NationalityCountryID = 1,
                    ImagePath = (string?)null
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Create_WhenNationalNumberConflicts_ReturnsConflict()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "The national number is already registered.";

        factory.PersonServiceMock
            .Setup(x =>
                x.AddPersonAsync(
                    It.IsAny<PersonCreateDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.PostAsJsonAsync(
                "api/People",
                new
                {
                    NationalNo = "9901234577",
                    FirstName = "Ahmad",
                    SecondName = "Mohammed",
                    ThirdName = (string?)null,
                    LastName = "Obeidat",
                    DateOfBirth = new DateTime(
                        1996,
                        5,
                        10),
                    Gender = (int)Gender.Male,
                    Address = "Amman",
                    Phone = "0791234567",
                    Email = "ahmad@example.com",
                    NationalityCountryID = 1,
                    ImagePath = (string?)null
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x =>
                x.UpdatePersonAsync(
                    55,
                    It.IsAny<PersonUpdateDto>()))
            .ReturnsAsync(
                Result.Success());

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.PutAsJsonAsync(
                "api/People/55",
                new
                {
                    NationalNo = "9901234555",
                    FirstName = "Updated",
                    SecondName = "Person",
                    ThirdName = "Ali",
                    LastName = "Obeidat",
                    DateOfBirth = new DateTime(
                        1995,
                        4,
                        20),
                    Gender = (int)Gender.Female,
                    Address = "Irbid",
                    Phone = "0781234567",
                    Email = "updated@example.com",
                    NationalityCountryID = 2,
                    ImagePath = "updated.jpg"
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.PersonServiceMock.Verify(
            x =>
                x.UpdatePersonAsync(
                    55,
                    It.Is<PersonUpdateDto>(
                        dto =>
                            dto.NationalNo == "9901234555" &&
                            dto.FirstName == "Updated" &&
                            dto.SecondName == "Person" &&
                            dto.ThirdName == "Ali" &&
                            dto.LastName == "Obeidat" &&
                            dto.DateOfBirth ==
                                new DateTime(1995, 4, 20) &&
                            dto.Gender == Gender.Female &&
                            dto.Address == "Irbid" &&
                            dto.Phone == "0781234567" &&
                            dto.Email == "updated@example.com" &&
                            dto.NationalityCountryID == 2 &&
                            dto.ImagePath == "updated.jpg")),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Person not found.";

        factory.PersonServiceMock
            .Setup(x =>
                x.UpdatePersonAsync(
                    55,
                    It.IsAny<PersonUpdateDto>()))
            .ReturnsAsync(
                Result.NotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.PutAsJsonAsync(
                "api/People/55",
                new
                {
                    NationalNo = "9901234555",
                    FirstName = "Updated",
                    SecondName = "Person",
                    ThirdName = (string?)null,
                    LastName = "Obeidat",
                    DateOfBirth = new DateTime(
                        1995,
                        4,
                        20),
                    Gender = (int)Gender.Female,
                    Address = "Irbid",
                    Phone = "0781234567",
                    Email = "updated@example.com",
                    NationalityCountryID = 2,
                    ImagePath = (string?)null
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Update_WhenNationalNumberConflicts_ReturnsConflict()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "The national number is already registered to another person.";

        factory.PersonServiceMock
            .Setup(x =>
                x.UpdatePersonAsync(
                    55,
                    It.IsAny<PersonUpdateDto>()))
            .ReturnsAsync(
                Result.Conflict(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.PutAsJsonAsync(
                "api/People/55",
                new
                {
                    NationalNo = "9901234555",
                    FirstName = "Updated",
                    SecondName = "Person",
                    ThirdName = (string?)null,
                    LastName = "Obeidat",
                    DateOfBirth = new DateTime(
                        1995,
                        4,
                        20),
                    Gender = (int)Gender.Female,
                    Address = "Irbid",
                    Phone = "0781234567",
                    Email = "updated@example.com",
                    NationalityCountryID = 2,
                    ImagePath = (string?)null
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x =>
                x.DeletePersonAsync(66))
            .ReturnsAsync(
                Result.Success());

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.DeleteAsync(
                "api/People/66");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.PersonServiceMock.Verify(
            x =>
                x.DeletePersonAsync(66),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenPersonHasApplications_ReturnsConflict()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Cannot delete this person because they have one or more applications.";

        factory.PersonServiceMock
            .Setup(x =>
                x.DeletePersonAsync(66))
            .ReturnsAsync(
                Result.Conflict(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.DeleteAsync(
                "api/People/66");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);

        factory.PersonServiceMock.Verify(
            x =>
                x.DeletePersonAsync(66),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Person not found.";

        factory.PersonServiceMock
            .Setup(x =>
                x.DeletePersonAsync(66))
            .ReturnsAsync(
                Result.NotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.DeleteAsync(
                "api/People/66");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Delete_WhenServiceFails_ReturnsInternalServerError()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Failed to delete person.";

        factory.PersonServiceMock
            .Setup(x =>
                x.DeletePersonAsync(66))
            .ReturnsAsync(
                Result.Failure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10);

        var response =
            await client.DeleteAsync(
                "api/People/66");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory,
        int userId)
    {
        var client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            userId.ToString());

        return client;
    }

    private static PersonDto CreatePersonDto(
        int id,
        string nationalNo,
        string firstName,
        string secondName,
        string? thirdName,
        string lastName,
        string fullName,
        Gender gender,
        string countryName)
    {
        return new PersonDto
        {
            PersonId = id,
            NationalNo = nationalNo,
            FirstName = firstName,
            SecondName = secondName,
            ThirdName = thirdName,
            LastName = lastName,
            FullName = fullName,
            DateOfBirth = new DateTime(
                1996,
                5,
                10),
            Gender = gender,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ahmad@example.com",
            NationalityCountryID = 1,
            CountryName = countryName,
            ImagePath = "ahmad.jpg"
        };
    }

    private sealed class ErrorResponse
    {
        public string Error { get; init; } =
            string.Empty;
    }
}