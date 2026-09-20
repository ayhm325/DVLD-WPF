using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Domain.Enums;
using DVLD.Contracts.Driver;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class DriversControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await factory.CreateClient().GetAsync("/api/Drivers")).StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuthentication_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        var drivers = new List<DriverDto>
        {
            new()
            {
                DriverID = 10, PersonID = 20, FullName = "Ahmad Mohammed",
                NationalNo = "123456789", DateOfBirth = new(1990, 5, 10),
                Gender = Gender.Male, ImagePath = "ahmad.jpg", ActiveLicenses = 2,
                CreatedByUserID = 5, CreatedByUserName = "admin",
                CreatedDate = DateTime.Parse("2026-09-10 14:30:00")
            }
        };

        factory.DriverServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<DriverDto>>.Success(drivers));

        var response = await Authenticated(factory).GetAsync("/api/Drivers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<DriverListResponse>>();

        Assert.NotNull(body);
        var driver = Assert.Single(body);

        Assert.Equal(10, driver.DriverId);
        Assert.Equal(20, driver.PersonId);
        Assert.Equal("Ahmad Mohammed", driver.FullName);
        Assert.Equal("123456789", driver.NationalNo);
        Assert.Equal(new DateTime(1990, 5, 10), driver.DateOfBirth);
        Assert.Equal(2, driver.ActiveLicenses);
        Assert.Equal(DateTime.Parse("2026-09-10 14:30:00"), driver.CreatedDate);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"gender\"", json);
        Assert.DoesNotContain("\"imagePath\"", json);
        Assert.DoesNotContain("\"createdByUserId\"", json);
        Assert.DoesNotContain("\"createdByUserName\"", json);
        factory.DriverServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.DriverServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<DriverDto>>.FromFailure("Failed to retrieve drivers."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/Drivers"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenDriverExists_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.GetByIdAsync(15))
            .ReturnsAsync(Result<DriverDto>.Success(new()
            {
                DriverID = 15,
                PersonID = 25,
                FullName = "Omar Ali",
                NationalNo = "987654321",
                DateOfBirth = new(1988, 3, 20),
                Gender = Gender.Male,
                ActiveLicenses = 1,
                CreatedByUserID = 7,
                CreatedByUserName = "staff",
                CreatedDate = new(2026, 8, 1)
            }));

        var response = await Authenticated(factory).GetAsync("/api/Drivers/15");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DriverResponse>();

        Assert.NotNull(body);
        Assert.Equal(15, body.DriverId);
        Assert.Equal(25, body.PersonId);
        Assert.Equal("Omar Ali", body.FullName);
        Assert.Equal("987654321", body.NationalNo);
        Assert.Equal("Male", body.Gender);
        Assert.Equal(1, body.ActiveLicenses);
    }

    [Fact]
    public async Task GetById_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.DriverServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync(Result<DriverDto>.FromNotFound("Driver not found."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/Drivers/999"),
            HttpStatusCode.NotFound, "Resource not found", "Driver not found.");
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.DriverServiceMock.Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(Result<DriverDto>.FromValidationFailure("Invalid driver ID."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/Drivers/0"),
            HttpStatusCode.BadRequest, "Validation error", "Invalid driver ID.");
    }

    [Fact]
    public async Task GetByPersonId_WhenDriverExists_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.GetByPersonIdAsync(40))
            .ReturnsAsync(Result<DriverDto>.Success(new()
            {
                DriverID = 30,
                PersonID = 40,
                FullName = "Khaled Hassan",
                NationalNo = "555555555",
                DateOfBirth = new(1992, 7, 15),
                Gender = Gender.Male,
                ActiveLicenses = 3,
                CreatedByUserID = 8,
                CreatedByUserName = "admin",
                CreatedDate = new(2026, 7, 1)
            }));

        var response = await Authenticated(factory).GetAsync("/api/Drivers/person/40");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DriverResponse>();

        Assert.NotNull(body);
        Assert.Equal(30, body.DriverId);
        Assert.Equal(40, body.PersonId);
        Assert.Equal("Khaled Hassan", body.FullName);
        Assert.Equal("555555555", body.NationalNo);
    }

    [Fact]
    public async Task GetByPersonId_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.DriverServiceMock.Setup(x => x.GetByPersonIdAsync(404))
            .ReturnsAsync(Result<DriverDto>.FromNotFound("Driver not found."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/Drivers/person/404"),
            HttpStatusCode.NotFound, "Resource not found", "Driver not found.");
    }

    [Fact]
    public async Task GetByCreatedUserId_ReturnsOkAndMapsList()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.GetByCreatedUserIdAsync(50))
            .ReturnsAsync(Result<List<DriverDto>>.Success(
            [
                new() { DriverID = 1, PersonID = 11, FullName = "First Driver", NationalNo = "111111111", Gender = Gender.Male, CreatedByUserID = 50, CreatedByUserName = "admin" },
                new() { DriverID = 2, PersonID = 12, FullName = "Second Driver", NationalNo = "222222222", Gender = Gender.Female, CreatedByUserID = 50, CreatedByUserName = "admin" }
            ]));

        var response = await Authenticated(factory).GetAsync("/api/Drivers/created-by/50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<DriverResponse>>();

        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal(1, body[0].DriverId);
        Assert.Equal("First Driver", body[0].FullName);
        Assert.Equal("Male", body[0].Gender);
        Assert.Equal(2, body[1].DriverId);
        Assert.Equal("Second Driver", body[1].FullName);
        Assert.Equal("Female", body[1].Gender);
    }

    [Fact]
    public async Task GetByCreatedUserId_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.DriverServiceMock.Setup(x => x.GetByCreatedUserIdAsync(0))
            .ReturnsAsync(Result<List<DriverDto>>.FromValidationFailure("Invalid created user ID."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).GetAsync("/api/Drivers/created-by/0"),
            HttpStatusCode.BadRequest, "Validation error", "Invalid created user ID.");
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreatedAndSendsCorrectDto()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.AddAsync(It.Is<CreateDriverDto>(d => d.PersonID == 123)))
            .ReturnsAsync(Result<int>.Success(77));

        var response = await Authenticated(factory)
            .PostAsJsonAsync("/api/Drivers", new { PersonId = 123 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("/api/Drivers/77", response.Headers.Location!.ToString());

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(77, document.RootElement.GetProperty("driverId").GetInt32());

        factory.DriverServiceMock.Verify(
            x => x.AddAsync(It.Is<CreateDriverDto>(d => d.PersonID == 123)), Times.Once);
    }

    [Theory]
    [InlineData("Person ID is invalid.", HttpStatusCode.BadRequest, "Validation error")]
    [InlineData("Person not found.", HttpStatusCode.NotFound, "Resource not found")]
    [InlineData("This person is already registered as a driver.", HttpStatusCode.Conflict, "Conflict")]
    public async Task Create_WhenServiceFails_ReturnsExpectedProblemDetails(
        string detail, HttpStatusCode status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        Result<int> result = status switch
        {
            HttpStatusCode.BadRequest => Result<int>.FromValidationFailure(detail),
            HttpStatusCode.NotFound => Result<int>.FromNotFound(detail),
            HttpStatusCode.Conflict => Result<int>.FromConflict(detail),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        factory.DriverServiceMock.Setup(x => x.AddAsync(It.IsAny<CreateDriverDto>()))
            .ReturnsAsync(result);

        var response = await Authenticated(factory)
            .PostAsJsonAsync("/api/Drivers", new { PersonId = 123 });

        await AssertProblemDetailsAsync(response, status, title, detail);
    }

    [Fact]
    public async Task Update_WhenRouteIdDoesNotMatchRequestId_ReturnsBadRequestAndDoesNotCallService()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await Authenticated(factory)
            .PutAsJsonAsync("/api/Drivers/10", new { DriverId = 20, PersonId = 50 });

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.BadRequest, "Validation error",
            "The route driver id does not match the request driver id.");

        factory.DriverServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<UpdateDriverDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenRequestIsValid_ReturnsNoContentAndSendsCorrectDto()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.UpdateAsync(It.Is<UpdateDriverDto>(
            d => d.DriverID == 20 && d.PersonID == 50)))
            .ReturnsAsync(Result.Success());

        var response = await Authenticated(factory)
            .PutAsJsonAsync("/api/Drivers/20", new { DriverId = 20, PersonId = 50 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        factory.DriverServiceMock.Verify(
            x => x.UpdateAsync(It.Is<UpdateDriverDto>(
                d => d.DriverID == 20 && d.PersonID == 50)), Times.Once);
    }

    [Theory]
    [InlineData("Driver not found.", HttpStatusCode.NotFound, "Resource not found")]
    [InlineData("This person is already registered as another driver.", HttpStatusCode.Conflict, "Conflict")]
    public async Task Update_WhenServiceFails_ReturnsExpectedProblemDetails(
        string detail, HttpStatusCode status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        Result result = status == HttpStatusCode.NotFound
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        factory.DriverServiceMock.Setup(x => x.UpdateAsync(It.IsAny<UpdateDriverDto>()))
            .ReturnsAsync(result);

        var response = await Authenticated(factory)
            .PutAsJsonAsync("/api/Drivers/20", new { DriverId = 20, PersonId = 50 });

        await AssertProblemDetailsAsync(response, status, title, detail);
    }

    [Fact]
    public async Task Delete_WhenDriverExists_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(Result.Success());

        var response = await Authenticated(factory).DeleteAsync("/api/Drivers/25");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        factory.DriverServiceMock.Verify(x => x.DeleteAsync(25), Times.Once);
    }

    [Theory]
    [InlineData("Driver not found.", HttpStatusCode.NotFound, "Resource not found")]
    [InlineData("Cannot delete a driver with existing licenses.", HttpStatusCode.Conflict, "Conflict")]
    public async Task Delete_WhenServiceReturnsExpectedFailure_ReturnsProblemDetails(
        string detail, HttpStatusCode status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        Result result = status == HttpStatusCode.NotFound
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        factory.DriverServiceMock.Setup(x => x.DeleteAsync(25)).ReturnsAsync(result);

        await AssertProblemDetailsAsync(
            await Authenticated(factory).DeleteAsync("/api/Drivers/25"),
            status, title, detail);
    }

    [Fact]
    public async Task Delete_WhenServiceFails_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.DriverServiceMock.Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(Result.Failure("Failed to delete driver."));

        await AssertProblemDetailsAsync(
            await Authenticated(factory).DeleteAsync("/api/Drivers/25"),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await factory.CreateClient().DeleteAsync("/api/Drivers/25")).StatusCode);
    }

    private static HttpClient Authenticated(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "1");
        return client;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response, HttpStatusCode status,
        string title, string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));

        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}