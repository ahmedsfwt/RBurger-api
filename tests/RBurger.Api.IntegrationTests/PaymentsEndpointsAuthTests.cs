using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §7.7: POST /payments/{orderId}/charge requires a Customer JWT; POST /payments/webhook is
// "Gateway signature (HMAC) - not user-authenticated" (public/anonymous). Mirrors
// OrdersEndpointsAuthTests' method-level-[Authorize] testing convention, since
// PaymentsController hosts both a Customer-only action and a deliberately anonymous one.
public class PaymentsEndpointsAuthTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public PaymentsEndpointsAuthTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Charge_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/payments/{Guid.NewGuid()}/charge", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Charge_with_Driver_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.PostAsync($"/api/v1/payments/{Guid.NewGuid()}/charge", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Charge_with_Admin_JWT_returns_403()
    {
        var client = _factory.CreateClient();
        var adminToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.PostAsync($"/api/v1/payments/{Guid.NewGuid()}/charge", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Proves a Customer JWT is not rejected by role authorization itself (opposite direction
    // of the 403 tests above), mirroring every other controller's identical "not rejected by
    // role authorization" test.
    [Fact]
    public async Task Post_Charge_with_Customer_JWT_is_not_rejected_by_role_authorization()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "key-1");

        var response = await client.PostAsync($"/api/v1/payments/{Guid.NewGuid()}/charge", null);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Charge_without_Idempotency_Key_returns_400()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.PostAsync($"/api/v1/payments/{Guid.NewGuid()}/charge", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // §7.7: the webhook route must remain reachable with no JWT at all - it must never return
    // 401/403 purely for lacking an Authorization header (a real gateway never sends one).
    [Fact]
    public async Task Post_Webhook_without_token_is_not_rejected_by_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/payments/webhook",
            new { transactionId = "TXN-1", orderReference = Guid.NewGuid().ToString(), status = "success", amount = 100 });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
