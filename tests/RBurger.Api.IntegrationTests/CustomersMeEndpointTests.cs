using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §5.4: "GET /api/v1/customers/me" requires a Customer JWT. This test verifies the
// documented authorization requirement is actually enforced by the pipeline, independent of
// any database being available (no token is sent, so the request never reaches the handler).
public class CustomersMeEndpointTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public CustomersMeEndpointTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_CustomersMe_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Day 9 hardening addition. §13.4: "a Driver JWT must receive 403 on every Customer- or
    // Admin-only route." [Authorize(Roles = "Customer")] is applied at the controller-class
    // level here (unlike OrdersController), so this also stands in for verifying that
    // class-level role gating is in effect for this controller.
    [Fact]
    public async Task Get_CustomersMe_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.GetAsync("/api/v1/customers/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
