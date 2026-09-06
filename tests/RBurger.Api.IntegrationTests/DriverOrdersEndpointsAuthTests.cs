using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §7.5 / §5.4: all five driver endpoints require a Driver JWT. These tests verify the
// documented authorization requirement is actually enforced by the pipeline - both the
// unauthenticated (401) and wrong-role (403) cases - independent of any database being
// available (a Customer JWT is rejected by [Authorize(Roles = "Driver")] before the request
// ever reaches a handler/repository).
public class DriverOrdersEndpointsAuthTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public DriverOrdersEndpointsAuthTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("GET", "/api/v1/driver/orders/new")]
    [InlineData("GET", "/api/v1/driver/orders/mine?status=active")]
    public async Task Driver_get_endpoints_without_token_return_401(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Receive_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/receive", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Ship_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/ship", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Deliver_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/deliver", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/driver/orders/new")]
    [InlineData("GET", "/api/v1/driver/orders/mine?status=active")]
    public async Task Driver_get_endpoints_with_Customer_JWT_return_403(string method, string url)
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Receive_with_Customer_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/receive", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Day 9 hardening additions: /receive above already proves DriverOrdersController's
    // class-level [Authorize(Roles = "Driver")] rejects a Customer JWT; these two close the
    // same check for the other two §7.5 POST actions for full per-route symmetry (§13.4).
    [Fact]
    public async Task Post_Ship_with_Customer_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/ship", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Deliver_with_Customer_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.PostAsync($"/api/v1/driver/orders/{Guid.NewGuid()}/deliver", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §7.4: GET /api/v1/orders/{orderId} now accepts either role (Day 7) - a Driver JWT must
    // not be rejected by the endpoint's own [Authorize] the way it would be for the other,
    // Customer-only §7.4 endpoints. (Ownership/assignment enforcement itself is covered by
    // GetOrderByIdQueryHandlerTests in RBurger.Application.Tests via in-memory fakes - this
    // integration test only proves routing/role-gating, not business ownership, since no DB
    // is available here.)
    [Fact]
    public async Task Get_OrderById_with_Driver_JWT_is_not_rejected_by_role_authorization()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        // Must NOT be 401/403 (those would indicate the Driver role is being rejected by
        // [Authorize] itself); the actual outcome without a database is a 500 from the
        // handler failing to reach SQL Server, which is expected and out of scope here.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §8.1: "Hub route: /hubs/orders". Confirms app.MapHub<OrdersHub> is actually wired into
    // the pipeline (and that AddSignalR()/the JWT OnMessageReceived event registration don't
    // break application startup). OrdersHub carries a bare [Authorize] (any authenticated
    // role), so an authenticated negotiate request against a mapped SignalR hub returns 200
    // with a connection payload; against an unmapped route it would be a 404 instead.
    [Fact]
    public async Task OrdersHub_negotiate_endpoint_is_mapped_and_reachable_when_authenticated()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.PostAsync("/hubs/orders/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Unauthenticated negotiate against the same (mapped, [Authorize]-protected) route is
    // rejected with 401, not the 404 a genuinely-unmapped route would return - together with
    // the test above, this proves the Hub is both mapped AND enforces authentication.
    [Fact]
    public async Task OrdersHub_negotiate_endpoint_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/hubs/orders/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDriverMine_with_invalid_status_and_no_token_still_returns_401_before_validation()
    {
        // Authorization runs before model binding/MediatR validation in the ASP.NET Core
        // pipeline, so an unauthenticated request is rejected with 401 regardless of the
        // (here, undocumented) status value supplied.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/driver/orders/mine?status=not-a-real-value");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
