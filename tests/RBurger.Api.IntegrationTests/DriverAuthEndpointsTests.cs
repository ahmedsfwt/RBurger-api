using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §7.2 / §1.2 / §13.4: Driver auth has login only - there is no signup endpoint, and §13.4
// requires a dedicated test asserting that route doesn't exist. §7.8: 404 is the documented
// status for POST /api/v1/auth/driver/signup specifically because it's absent by design.
//
// Following the same convention as CustomersMeEndpointTests/OrdersEndpointsAuthTests: these
// tests only exercise routing and the FluentValidation pipeline (which runs before any
// repository/DB access, per ValidationBehavior), so they run independent of any database being
// available. Credential-verification and business-rule behavior (401/403/200) are covered by
// DriverLoginCommandHandlerTests in RBurger.Application.Tests, using in-memory fakes.
public class DriverAuthEndpointsTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public DriverAuthEndpointsTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_DriverSignup_route_does_not_exist_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/driver/signup", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_DriverLogin_with_empty_body_returns_400_validation_error()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/driver/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
