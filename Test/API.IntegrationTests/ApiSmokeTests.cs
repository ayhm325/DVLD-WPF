using API.IntegrationTests.Infrastructure;

namespace API.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public async Task Api_CanStart()
    {
        await using var factory = new ApiWebApplicationFactory();

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}