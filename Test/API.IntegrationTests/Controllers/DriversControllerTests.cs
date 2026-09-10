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
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Drivers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAuthentication_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var createdDate = new DateTime(2026, 9, 10, 14, 30, 0);

        var drivers = new List<DriverDto>
        {
            new()
            {
                DriverID = 10,
                PersonID = 20,
                FullName = "Ahmad Mohammed",
                NationalNo = "123456789",
                DateOfBirth = new DateTime(1990, 5, 10),
                Gender = Gender.Male,
                ImagePath = "ahmad.jpg",
                ActiveLicenses = 2,
                CreatedByUserID = 5,
                CreatedByUserName = "admin",
                CreatedDate = createdDate
            }
        };

        factory.DriverServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(Result<List<DriverDto>>.Success(drivers));

        var response = await client.GetAsync("/api/Drivers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<DriverResponse>>();

        Assert.NotNull(body);
        var driver = Assert.Single(body);

        Assert.Equal(10, driver.DriverId);
        Assert.Equal(20, driver.PersonId);
        Assert.Equal("Ahmad Mohammed", driver.FullName);
        Assert.Equal("123456789", driver.NationalNo);
        Assert.Equal(new DateTime(1990, 5, 10), driver.DateOfBirth);
        Assert.Equal("Male", driver.Gender);
        Assert.Equal("ahmad.jpg", driver.ImagePath);
        Assert.Equal(2, driver.ActiveLicenses);
        Assert.Equal(5, driver.CreatedByUserId);
        Assert.Equal("admin", driver.CreatedByUserName);
        Assert.Equal(createdDate, driver.CreatedDate);

        factory.DriverServiceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceFails_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                Result<List<DriverDto>>.FromFailure(
                    "Failed to retrieve drivers."));

        var response = await client.GetAsync("/api/Drivers");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Failed to retrieve drivers.");
    }

    [Fact]
    public async Task GetById_WhenDriverExists_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var driver = new DriverDto
        {
            DriverID = 15,
            PersonID = 25,
            FullName = "Omar Ali",
            NationalNo = "987654321",
            DateOfBirth = new DateTime(1988, 3, 20),
            Gender = Gender.Male,
            ImagePath = null,
            ActiveLicenses = 1,
            CreatedByUserID = 7,
            CreatedByUserName = "staff",
            CreatedDate = new DateTime(2026, 8, 1)
        };

        factory.DriverServiceMock
            .Setup(x => x.GetByIdAsync(15))
            .ReturnsAsync(Result<DriverDto>.Success(driver));

        var response = await client.GetAsync("/api/Drivers/15");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DriverResponse>();

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
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync(
                Result<DriverDto>.FromNotFound(
                    "Driver not found."));

        var response = await client.GetAsync("/api/Drivers/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Driver not found.");
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.GetByIdAsync(0))
            .ReturnsAsync(
                Result<DriverDto>.FromValidationFailure(
                    "Invalid driver ID."));

        var response = await client.GetAsync("/api/Drivers/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid driver ID.");
    }

    [Fact]
    public async Task GetByPersonId_WhenDriverExists_ReturnsOkAndMapsResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var driver = new DriverDto
        {
            DriverID = 30,
            PersonID = 40,
            FullName = "Khaled Hassan",
            NationalNo = "555555555",
            DateOfBirth = new DateTime(1992, 7, 15),
            Gender = Gender.Male,
            ActiveLicenses = 3,
            CreatedByUserID = 8,
            CreatedByUserName = "admin",
            CreatedDate = new DateTime(2026, 7, 1)
        };

        factory.DriverServiceMock
            .Setup(x => x.GetByPersonIdAsync(40))
            .ReturnsAsync(Result<DriverDto>.Success(driver));

        var response =
            await client.GetAsync("/api/Drivers/person/40");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DriverResponse>();

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
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.GetByPersonIdAsync(404))
            .ReturnsAsync(
                Result<DriverDto>.FromNotFound(
                    "Driver not found."));

        var response =
            await client.GetAsync("/api/Drivers/person/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Driver not found.");
    }

    [Fact]
    public async Task GetByCreatedUserId_ReturnsOkAndMapsList()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var drivers = new List<DriverDto>
        {
            new()
            {
                DriverID = 1,
                PersonID = 11,
                FullName = "First Driver",
                NationalNo = "111111111",
                Gender = Gender.Male,
                CreatedByUserID = 50,
                CreatedByUserName = "admin"
            },
            new()
            {
                DriverID = 2,
                PersonID = 12,
                FullName = "Second Driver",
                NationalNo = "222222222",
                Gender = Gender.Female,
                CreatedByUserID = 50,
                CreatedByUserName = "admin"
            }
        };

        factory.DriverServiceMock
            .Setup(x => x.GetByCreatedUserIdAsync(50))
            .ReturnsAsync(Result<List<DriverDto>>.Success(drivers));

        var response =
            await client.GetAsync("/api/Drivers/created-by/50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<DriverResponse>>();

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
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.GetByCreatedUserIdAsync(0))
            .ReturnsAsync(
                Result<List<DriverDto>>.FromValidationFailure(
                    "Invalid created user ID."));

        var response =
            await client.GetAsync("/api/Drivers/created-by/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Invalid created user ID.");
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreatedAndSendsCorrectDto()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.AddAsync(
                It.Is<CreateDriverDto>(
                    dto => dto.PersonID == 123)))
            .ReturnsAsync(Result<int>.Success(77));

        var request = new
        {
            PersonId = 123
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Drivers",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains(
            "/api/Drivers/77",
            response.Headers.Location!.ToString());

        using var document =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        Assert.Equal(
            77,
            document.RootElement
                .GetProperty("driverId")
                .GetInt32());

        factory.DriverServiceMock.Verify(
            x => x.AddAsync(
                It.Is<CreateDriverDto>(
                    dto => dto.PersonID == 123)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateDriverDto>()))
            .ReturnsAsync(
                Result<int>.FromValidationFailure(
                    "Person ID is invalid."));

        var response =
            await client.PostAsJsonAsync(
                "/api/Drivers",
                new
                {
                    PersonId = 0
                });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Person ID is invalid.");
    }

    [Fact]
    public async Task Create_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateDriverDto>()))
            .ReturnsAsync(
                Result<int>.FromNotFound(
                    "Person not found."));

        var response =
            await client.PostAsJsonAsync(
                "/api/Drivers",
                new
                {
                    PersonId = 999
                });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await AssertErrorAsync(
            response,
            "Person not found.");
    }

    [Fact]
    public async Task Create_WhenServiceReturnsConflict_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.AddAsync(It.IsAny<CreateDriverDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "This person is already registered as a driver."));

        var response =
            await client.PostAsJsonAsync(
                "/api/Drivers",
                new
                {
                    PersonId = 123
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "This person is already registered as a driver.");
    }

    [Fact]
    public async Task Update_WhenRouteIdDoesNotMatchRequestId_ReturnsBadRequestAndDoesNotCallService()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var request = new
        {
            DriverId = 20,
            PersonId = 50
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/Drivers/10",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "The route driver id does not match the request driver id.");

        factory.DriverServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<UpdateDriverDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_WhenRequestIsValid_ReturnsNoContentAndSendsCorrectDto()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.UpdateAsync(
                It.Is<UpdateDriverDto>(
                    dto =>
                        dto.DriverID == 20 &&
                        dto.PersonID == 50)))
            .ReturnsAsync(Result.Success());

        var request = new
        {
            DriverId = 20,
            PersonId = 50
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/Drivers/20",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.DriverServiceMock.Verify(
            x => x.UpdateAsync(
                It.Is<UpdateDriverDto>(
                    dto =>
                        dto.DriverID == 20 &&
                        dto.PersonID == 50)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateDriverDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "Driver not found."));

        var response =
            await client.PutAsJsonAsync(
                "/api/Drivers/20",
                new
                {
                    DriverId = 20,
                    PersonId = 50
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Driver not found.");
    }

    [Fact]
    public async Task Update_WhenServiceReturnsConflict_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.UpdateAsync(It.IsAny<UpdateDriverDto>()))
            .ReturnsAsync(
                Result.Conflict(
                    "This person is already registered as another driver."));

        var response =
            await client.PutAsJsonAsync(
                "/api/Drivers/20",
                new
                {
                    DriverId = 20,
                    PersonId = 50
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "This person is already registered as another driver.");
    }

    [Fact]
    public async Task Delete_WhenDriverExists_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(Result.Success());

        var response =
            await client.DeleteAsync("/api/Drivers/25");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.DriverServiceMock.Verify(
            x => x.DeleteAsync(25),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenDriverDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(
                Result.NotFound(
                    "Driver not found."));

        var response =
            await client.DeleteAsync("/api/Drivers/25");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Driver not found.");
    }

    [Fact]
    public async Task Delete_WhenDriverHasLicenses_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(
                Result.Conflict(
                    "Cannot delete a driver with existing licenses."));

        var response =
            await client.DeleteAsync("/api/Drivers/25");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Cannot delete a driver with existing licenses.");
    }

    [Fact]
    public async Task Delete_WhenServiceFails_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        factory.DriverServiceMock
            .Setup(x => x.DeleteAsync(25))
            .ReturnsAsync(
                Result.Failure(
                    "Failed to delete driver."));

        var response =
            await client.DeleteAsync("/api/Drivers/25");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        await AssertErrorAsync(
            response,
            "Failed to delete driver.");
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response =
            await client.DeleteAsync("/api/Drivers/25");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            "1");

        return client;
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response,
        string expectedError)
    {
        using var document =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        var error =
            document.RootElement
                .GetProperty("error")
                .GetString();

        Assert.Equal(expectedError, error);
    }
}