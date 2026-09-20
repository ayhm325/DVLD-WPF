using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Domain.Enums;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class PeopleControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/People");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.PersonServiceMock.Verify(x => x.GetAllPeopleAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticated_ReturnsMappedPeople()
    {
        await using var factory = new ApiWebApplicationFactory();

        var people = new List<PersonDto>
        {
            CreatePersonDto(1, "9901234567", "Ahmad", "Mohammed", "Ali",
                "Obeidat", "Ahmad Mohammed Ali Obeidat", (int)Gender.Male, "Jordan"),
            CreatePersonDto(2, "9901234568", "Sara", "Mohammed", null,
                "Ali", "Sara Mohammed Ali", (int)Gender.Female, "Jordan")
        };

        factory.PersonServiceMock.Setup(x => x.GetAllPeopleAsync())
            .ReturnsAsync(Result<List<PersonDto>>.Success(people));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("/api/People");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
        Assert.Equal(1, root[0].GetProperty("personId").GetInt32());
        Assert.Equal("9901234567", root[0].GetProperty("nationalNo").GetString());
        Assert.Equal("Ahmad Mohammed Ali Obeidat", root[0].GetProperty("fullName").GetString());
        Assert.Equal("Amman", root[0].GetProperty("address").GetString());
        Assert.Equal("0791234567", root[0].GetProperty("phone").GetString());
        Assert.Equal("ahmad@example.com", root[0].GetProperty("email").GetString());
        Assert.Equal("Jordan", root[0].GetProperty("countryName").GetString());

        factory.PersonServiceMock.Verify(x => x.GetAllPeopleAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock.Setup(x => x.GetAllPeopleAsync())
            .ReturnsAsync(Result<List<PersonDto>>.FromFailure(
                "Unexpected person retrieval failure."));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("/api/People");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenPersonExists_ReturnsMappedPerson()
    {
        await using var factory = new ApiWebApplicationFactory();

        var person = CreatePersonDto(
            25, "9901234599", "Ahmad", "Mohammed", "Ali",
            "Obeidat", "Ahmad Mohammed Ali Obeidat",
            (int)Gender.Male, "Jordan");

        factory.PersonServiceMock.Setup(x => x.GetPersonByIdAsync(25))
            .ReturnsAsync(Result<PersonDto>.Success(person));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("/api/People/25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal(25, body.GetProperty("personId").GetInt32());
        Assert.Equal("9901234599", body.GetProperty("nationalNo").GetString());
        Assert.Equal("Ahmad Mohammed Ali Obeidat", body.GetProperty("fullName").GetString());
        Assert.Equal("Jordan", body.GetProperty("countryName").GetString());

        factory.PersonServiceMock.Verify(x => x.GetPersonByIdAsync(25), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenPersonDoesNotExist_Returns404ProblemDetails()
    {
        const string detail = "Person not found.";

        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock.Setup(x => x.GetPersonByIdAsync(999))
            .ReturnsAsync(Result<PersonDto>.FromNotFound(detail));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("/api/People/999");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);

        factory.PersonServiceMock.Verify(x => x.GetPersonByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetByNationalNo_WhenPersonExists_ReturnsMappedPerson()
    {
        const string nationalNo = "9901234567";

        await using var factory = new ApiWebApplicationFactory();

        var person = CreatePersonDto(
            30, nationalNo, "Khaled", "Ali", null,
            "Omar", "Khaled Ali Omar",
            (int)Gender.Male, "Jordan");

        factory.PersonServiceMock.Setup(
                x => x.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync(Result<PersonDto>.Success(person));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync($"/api/People/national/{nationalNo}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal(30, body.GetProperty("personId").GetInt32());
        Assert.Equal(nationalNo, body.GetProperty("nationalNo").GetString());
        Assert.Equal("Khaled Ali Omar", body.GetProperty("fullName").GetString());

        factory.PersonServiceMock.Verify(
            x => x.GetPersonByNationalNoAsync(nationalNo), Times.Once);
    }

    [Fact]
    public async Task GetByNationalNo_WhenPersonDoesNotExist_Returns404ProblemDetails()
    {
        const string nationalNo = "9999999999";
        const string detail = "Person not found.";

        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock.Setup(
                x => x.GetPersonByNationalNoAsync(nationalNo))
            .ReturnsAsync(Result<PersonDto>.FromNotFound(detail));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync(
            $"/api/People/national/{nationalNo}");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            detail);
    }

    [Fact]
    public async Task Create_WhenServiceSucceeds_Returns201WithPersonId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x => x.AddPersonAsync(It.IsAny<PersonCreateDto>()))
            .ReturnsAsync(Result<int>.Success(77));

        using var client = CreateAuthenticatedClient(factory, 10);

        var request = new
        {
            NationalNo = "9901234577",
            FirstName = "Ahmad",
            SecondName = "Mohammed",
            ThirdName = "Ali",
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1996, 5, 10),
            Gender = (int)Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ahmad@example.com",
            NationalityCountryID = 1,
            ImagePath = "ahmad.jpg"
        };

        var response = await client.PostAsJsonAsync("/api/People", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(77, document.RootElement.GetProperty("personId").GetInt32());
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/People/77", response.Headers.Location.ToString());

        factory.PersonServiceMock.Verify(
            x => x.AddPersonAsync(It.Is<PersonCreateDto>(dto =>
                dto.NationalNo == "9901234577" &&
                dto.FirstName == "Ahmad" &&
                dto.SecondName == "Mohammed" &&
                dto.ThirdName == "Ali" &&
                dto.LastName == "Obeidat" &&
                dto.DateOfBirth == new DateTime(1996, 5, 10) &&
                dto.Gender == (int)Gender.Male &&
                dto.Address == "Amman" &&
                dto.Phone == "0791234567" &&
                dto.Email == "ahmad@example.com" &&
                dto.NationalityCountryID == 1 &&
                dto.ImagePath == "ahmad.jpg")),
            Times.Once);
    }

    [Theory]
    [InlineData(400, "Validation error", "National number is required.")]
    [InlineData(409, "Conflict", "The national number is already registered.")]
    public async Task Create_WhenResultFails_ReturnsProblemDetails(
        int statusCode,
        string title,
        string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = statusCode == 400
            ? Result<int>.FromValidationFailure(detail)
            : Result<int>.FromConflict(detail);

        factory.PersonServiceMock
            .Setup(x => x.AddPersonAsync(It.IsAny<PersonCreateDto>()))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory, 10);

        var response = await client.PostAsJsonAsync("/api/People", new
        {
            NationalNo = statusCode == 400 ? "" : "9901234577",
            FirstName = "Ahmad",
            SecondName = "Mohammed",
            ThirdName = (string?)null,
            LastName = "Obeidat",
            DateOfBirth = new DateTime(1996, 5, 10),
            Gender = (int)Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ahmad@example.com",
            NationalityCountryID = 1,
            ImagePath = (string?)null
        });

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task Update_WhenServiceSucceeds_Returns204()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x => x.UpdatePersonAsync(55, It.IsAny<PersonUpdateDto>()))
            .ReturnsAsync(Result.Success());

        using var client = CreateAuthenticatedClient(factory, 10);

        var response = await client.PutAsJsonAsync("/api/People/55", CreateUpdateRequest());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.PersonServiceMock.Verify(
            x => x.UpdatePersonAsync(55, It.Is<PersonUpdateDto>(dto =>
                dto.NationalNo == "9901234555" &&
                dto.FirstName == "Updated" &&
                dto.SecondName == "Person" &&
                dto.ThirdName == "Ali" &&
                dto.LastName == "Obeidat" &&
                dto.DateOfBirth == new DateTime(1995, 4, 20) &&
                dto.Gender == (int)Gender.Female &&
                dto.Address == "Irbid" &&
                dto.Phone == "0781234567" &&
                dto.Email == "updated@example.com" &&
                dto.NationalityCountryID == 2 &&
                dto.ImagePath == "updated.jpg")),
            Times.Once);
    }

    [Theory]
    [InlineData(404, "Resource not found", "Person not found.")]
    [InlineData(409, "Conflict",
        "The national number is already registered to another person.")]
    public async Task Update_WhenResultFails_ReturnsProblemDetails(
        int statusCode,
        string title,
        string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = statusCode == 404
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        factory.PersonServiceMock
            .Setup(x => x.UpdatePersonAsync(55, It.IsAny<PersonUpdateDto>()))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PutAsJsonAsync(
            "/api/People/55",
            CreateUpdateRequest());

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_Returns204()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x => x.DeletePersonAsync(66))
            .ReturnsAsync(Result.Success());

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.DeleteAsync("/api/People/66");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.PersonServiceMock.Verify(
            x => x.DeletePersonAsync(66), Times.Once);
    }

    [Theory]
    [InlineData(404, "Resource not found", "Person not found.")]
    [InlineData(409, "Conflict",
        "Cannot delete this person because they have one or more applications.")]
    public async Task Delete_WhenResultFails_ReturnsProblemDetails(
        int statusCode,
        string title,
        string detail)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = statusCode == 404
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        factory.PersonServiceMock
            .Setup(x => x.DeletePersonAsync(66))
            .ReturnsAsync(result);

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.DeleteAsync("/api/People/66");

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)statusCode,
            title,
            detail);

        factory.PersonServiceMock.Verify(
            x => x.DeletePersonAsync(66), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceFails_Returns500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x => x.DeletePersonAsync(66))
            .ReturnsAsync(Result.Failure("Failed to delete person."));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.DeleteAsync("/api/People/66");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory,
        int userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        return client;
    }

    private static object CreateUpdateRequest() => new
    {
        NationalNo = "9901234555",
        FirstName = "Updated",
        SecondName = "Person",
        ThirdName = "Ali",
        LastName = "Obeidat",
        DateOfBirth = new DateTime(1995, 4, 20),
        Gender = (int)Gender.Female,
        Address = "Irbid",
        Phone = "0781234567",
        Email = "updated@example.com",
        NationalityCountryID = 2,
        ImagePath = "updated.jpg"
    };

    private static PersonDto CreatePersonDto(
        int id,
        string nationalNo,
        string firstName,
        string secondName,
        string? thirdName,
        string lastName,
        string fullName,
        int gender,
        string countryName) => new()
        {
            PersonId = id,
            NationalNo = nationalNo,
            FirstName = firstName,
            SecondName = secondName,
            ThirdName = thirdName,
            LastName = lastName,
            FullName = fullName,
            DateOfBirth = new DateTime(1996, 5, 10),
            Gender = gender,
            Address = "Amman",
            Phone = "0791234567",
            Email = "ahmad@example.com",
            NationalityCountryID = 1,
            CountryName = countryName,
            ImagePath = "ahmad.jpg"
        };

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));

        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
