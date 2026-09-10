using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using DVLD.Contracts.TestType;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class TestTypesControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestTypesControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.TestTypeServiceMock.Reset();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var dto = new TestTypeDto
        {
            TestTypeId = 1,
            TestTypeTitle = "Vision Test",
            TestTypeDescription = "Vision examination",
            TestTypeFees = 10m
        };

        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(
                Result<List<TestTypeDto>>.Success(
                    new List<TestTypeDto>
                    {
                        dto
                    }));

        var response =
            await _client.GetAsync(
                "/api/TestTypes");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<TestTypeResponse>>();

        Assert.NotNull(result);

        var item = Assert.Single(result);

        Assert.Equal(1, item.TestTypeId);
        Assert.Equal(
            "Vision Test",
            item.TestTypeTitle);
        Assert.Equal(
            "Vision examination",
            item.TestTypeDescription);
        Assert.Equal(
            10m,
            item.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetAllTestTypesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequestWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(
                Result<List<TestTypeDto>>
                    .FromValidationFailure(
                        "validation error"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFoundWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(
                Result<List<TestTypeDto>>
                    .FromNotFound(
                        "not found"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "not found");
    }

    [Fact]
    public async Task GetAll_WhenConflict_ReturnsConflictWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(
                Result<List<TestTypeDto>>
                    .FromConflict(
                        "conflict"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task GetAll_WhenFailureOccurs_ReturnsBadRequestWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetAllTestTypesAsync())
            .ReturnsAsync(
                Result<List<TestTypeDto>>
                    .FromFailure(
                        "failure"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes");

        // This is intentional:
        // TestTypesController maps unknown ErrorType values to 400.
        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "failure");
    }

    [Fact]
    public async Task GetById_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new TestTypeDto
        {
            TestTypeId = 5,
            TestTypeTitle = "Theory Test",
            TestTypeDescription =
                "Written theoretical examination",
            TestTypeFees = 15m
        };

        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(
                Result<TestTypeDto>.Success(dto));

        var response =
            await _client.GetAsync(
                "/api/TestTypes/5");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<TestTypeResponse>();

        Assert.NotNull(result);

        Assert.Equal(5, result.TestTypeId);
        Assert.Equal(
            "Theory Test",
            result.TestTypeTitle);
        Assert.Equal(
            "Written theoretical examination",
            result.TestTypeDescription);
        Assert.Equal(
            15m,
            result.TestTypeFees);

        _factory.TestTypeServiceMock.Verify(
            x => x.GetTestTypeByIdAsync(5),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFoundWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(
                Result<TestTypeDto>
                    .FromNotFound(
                        "test type not found"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes/5");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "test type not found");
    }

    [Fact]
    public async Task GetById_WhenValidationFails_ReturnsBadRequestWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x => x.GetTestTypeByIdAsync(5))
            .ReturnsAsync(
                Result<TestTypeDto>
                    .FromValidationFailure(
                        "validation error"));

        var response =
            await _client.GetAsync(
                "/api/TestTypes/5");

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsNoContent()
    {
        _factory.TestTypeServiceMock
            .Setup(x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.Is<TestTypeDto>(
                        d =>
                            d.TestTypeId == 5 &&
                            d.TestTypeTitle ==
                                "Updated Theory Test" &&
                            d.TestTypeDescription ==
                                "Updated description" &&
                            d.TestTypeFees == 20m)))
            .ReturnsAsync(
                Result.Success());

        var request =
            new UpdateTestTypeRequest
            {
                TestTypeId = 999,
                TestTypeTitle = "Updated Theory Test",
                TestTypeDescription =
                    "Updated description",
                TestTypeFees = 20m
            };

        var response =
            await SendUpdateAsync(
                5,
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        _factory.TestTypeServiceMock.Verify(
            x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.Is<TestTypeDto>(
                        d =>
                            d.TestTypeId == 5 &&
                            d.TestTypeTitle ==
                                "Updated Theory Test" &&
                            d.TestTypeDescription ==
                                "Updated description" &&
                            d.TestTypeFees == 20m)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFoundWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.IsAny<TestTypeDto>()))
            .ReturnsAsync(
                Result.NotFound(
                    "test type not found"));

        var request =
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            };

        var response =
            await SendUpdateAsync(
                5,
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "test type not found");
    }

    [Fact]
    public async Task Update_WhenConflictOccurs_ReturnsConflictWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.IsAny<TestTypeDto>()))
            .ReturnsAsync(
                Result.Conflict(
                    "conflict"));

        var request =
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            };

        var response =
            await SendUpdateAsync(
                5,
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequestWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.IsAny<TestTypeDto>()))
            .ReturnsAsync(
                Result.ValidationFailure(
                    "validation error"));

        var request =
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            };

        var response =
            await SendUpdateAsync(
                5,
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task Update_WhenFailureOccurs_ReturnsBadRequestWithMessage()
    {
        _factory.TestTypeServiceMock
            .Setup(x =>
                x.UpdateTestTypeAsync(
                    5,
                    It.IsAny<TestTypeDto>()))
            .ReturnsAsync(
                Result.Failure(
                    "failure"));

        var request =
            new UpdateTestTypeRequest
            {
                TestTypeId = 5,
                TestTypeTitle = "Updated",
                TestTypeDescription = "Description",
                TestTypeFees = 20m
            };

        var response =
            await SendUpdateAsync(
                5,
                request);

        await AssertFailureResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "failure");
    }

    private async Task<HttpResponseMessage>
        SendUpdateAsync(
            int id,
            UpdateTestTypeRequest request)
    {
        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/TestTypes/{id}");

        httpRequest.Content =
            JsonContent.Create(request);

        return await _client.SendAsync(
            httpRequest);
    }

    private static async Task
        AssertFailureResponseAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatus,
            string expectedMessage)
    {
        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            expectedMessage,
            body.Message);
    }

    private sealed record ErrorResponse(
        string? Message);
}