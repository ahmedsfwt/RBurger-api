using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §5.4: all five §7.4 Orders endpoints require a Customer JWT (three from Day 4, two added in
// Day 5). These tests verify the documented authorization requirement is actually enforced by
// the pipeline, independent of any database being available (no token is sent, so the request
// never reaches a handler).
public class OrdersEndpointsAuthTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public OrdersEndpointsAuthTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Orders_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/orders", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_OrdersMine_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/orders/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_OrderById_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Day 5 addition.
    [Fact]
    public async Task Post_CustomerReceived_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/orders/{Guid.NewGuid()}/customer-received", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Day 5 addition.
    [Fact]
    public async Task Post_Review_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/review", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Day 9 hardening additions ----
    //
    // §13.4: "a Driver JWT must receive 403 on every Customer- or Admin-only route ... and
    // vice versa for Admin-only routes against Customer/Driver JWTs." DriverOrdersEndpointsAuthTests
    // already covers the reverse direction (Customer JWT -> Driver-only routes = 403); this closes
    // the gap for these four Customer-only §7.4 actions, which each carry their own
    // [Authorize(Roles = "Customer")] (OrdersController is not class-level [Authorize(Roles=...)],
    // per its own header comment, so each action's role gate must be verified independently).
    // GET /orders/{orderId} is deliberately excluded here since §7.4 documents it as
    // Customer-or-Driver accessible - that shared-access behavior is already verified by
    // GetOrderById_with_Driver_JWT_is_not_rejected_by_role_authorization in
    // DriverOrdersEndpointsAuthTests.

    [Fact]
    public async Task Post_Orders_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.PostAsJsonAsync("/api/v1/orders", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_OrdersMine_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.GetAsync("/api/v1/orders/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_CustomerReceived_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.PostAsync($"/api/v1/orders/{Guid.NewGuid()}/customer-received", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Review_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/review", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §13.4 "invalid JWT": a token signed with a key the API does not trust must fail
    // authentication itself (401), never fall through to authorization/handler logic - this is
    // distinct from the "without token" tests above (no Authorization header at all) and from
    // DriverAuthEndpointsTests' 400 (a validation failure on an *unauthenticated* public route).
    [Fact]
    public async Task Get_OrdersMine_with_token_signed_by_wrong_key_returns_401()
    {
        var client = _factory.CreateClient();
        var tamperedToken = TestJwtFactory.CreateTokenWithWrongSigningKey(Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/v1/orders/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
