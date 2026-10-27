using System.Net;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class HealthEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Health_returns_200_when_postgres_is_reachable()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Healthy");
    }
}
