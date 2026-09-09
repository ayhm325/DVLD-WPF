using Application.DTOs.CountryDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class CountryServiceTests
{
    private readonly Mock<ICountryRepository> _repository = new();

    private CountryService CreateService() =>
        new(_repository.Object);

    private static Country CreateEntity(
        int id = 1,
        string name = "Jordan")
        => new()
        {
            CountryId = id,
            CountryName = name
        };

    [Fact]
    public async Task GetAllCountriesAsync_WhenRepositoryReturnsCountries_ReturnsSuccess()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
            [
                CreateEntity(1, "Jordan"),
                CreateEntity(2, "Saudi Arabia"),
                CreateEntity(3, "Palestine")
            ]);

        var result = await service.GetAllCountriesAsync();

        Assert.True(result.IsSuccess);

        var countries = result.Value
            ?? throw new Xunit.Sdk.XunitException("Expected country list.");

        Assert.Equal(3, countries.Count);

        Assert.Equal(1, countries[0].CountryId);
        Assert.Equal("Jordan", countries[0].CountryName);

        Assert.Equal(2, countries[1].CountryId);
        Assert.Equal("Saudi Arabia", countries[1].CountryName);

        Assert.Equal(3, countries[2].CountryId);
        Assert.Equal("Palestine", countries[2].CountryName);

        _repository.Verify(
            x => x.GetAllCountriesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllCountriesAsync_WhenRepositoryReturnsEmpty_ReturnsSuccessWithEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync([]);

        var result = await service.GetAllCountriesAsync();

        Assert.True(result.IsSuccess);

        var countries = result.Value
            ?? throw new Xunit.Sdk.XunitException("Expected country list.");

        Assert.Empty(countries);

        _repository.Verify(
            x => x.GetAllCountriesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllCountriesAsync_PreservesRepositoryOrder()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
            [
                CreateEntity(10, "Z"),
                CreateEntity(2, "A"),
                CreateEntity(7, "M")
            ]);

        var result = await service.GetAllCountriesAsync();

        Assert.True(result.IsSuccess);

        var countries = result.Value
            ?? throw new Xunit.Sdk.XunitException("Expected country list.");

        Assert.Equal(10, countries[0].CountryId);
        Assert.Equal(2, countries[1].CountryId);
        Assert.Equal(7, countries[2].CountryId);
    }

    [Fact]
    public async Task GetAllCountriesAsync_MapsEveryEntityToDto()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
            [
                CreateEntity(100, "Jordan")
            ]);

        var result = await service.GetAllCountriesAsync();

        Assert.True(result.IsSuccess);

        var countries = result.Value
            ?? throw new Xunit.Sdk.XunitException("Expected country list.");

        var dto = Assert.Single(countries);

        Assert.IsType<Application.DTOs.CountryDTO.CountryDto>(dto);
        Assert.Equal(100, dto.CountryId);
        Assert.Equal("Jordan", dto.CountryName);
    }

    [Fact]
    public async Task GetAllCountriesAsync_WhenRepositoryThrows_PropagatesException()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ThrowsAsync(
                new InvalidOperationException("Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetAllCountriesAsync());

        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task GetAllCountriesAsync_WhenRepositoryReturnsNullList_ThrowsArgumentNullException()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync((List<Country>)null!);

        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetAllCountriesAsync());

        Assert.Equal("source", exception.ParamName);
    }
}
