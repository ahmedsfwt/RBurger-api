using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RBurger.Infrastructure.Authentication;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// Day 16 addition - deterministic regression test for the root-cause fix described in
// Program.cs's "Day 16 CRITICAL FIX" comment: JwtSettings must be resolved lazily (post-Build)
// so the real JwtBearer pipeline validates tokens against the SAME key TestJwtFactory signs
// them with. This exercises the REAL JwtBearer middleware end-to-end (no fake auth scheme) -
// if the config-timing bug ever regresses, every test in this class fails with 401.
public class AuthenticationPipelineTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public AuthenticationPipelineTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // §5.4: role-protected endpoint reached only with the exact matching role. Uses
    // GET /api/v1/customers/me (Customer-only) as the proof point - reaching MediatR at all
    // (rather than short-circuiting at [Authorize]) proves both authentication AND the "sub"
    // claim resolved correctly, since ICurrentUserService.CustomerId feeds directly from it.
    [Fact]
    public async Task Valid_Customer_JWT_authenticates_and_is_not_rejected_by_the_pipeline()
    {
        var client = _factory.CreateClient();
        var token = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/customers/me");

        // Not 401/403 - the token authenticated as Customer and the role check passed. (May
        // still be 404/500 downstream since no such customer exists in a real DB during this
        // routing-only test - the point is the auth pipeline itself let the request through.)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Valid_Driver_JWT_authenticates_and_is_not_rejected_by_the_pipeline()
    {
        var client = _factory.CreateClient();
        var token = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/driver/orders/new");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Valid_Admin_JWT_authenticates_and_is_not_rejected_by_the_pipeline()
    {
        var client = _factory.CreateClient();
        var token = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/customers");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Proves TestJwtFactory and the real JwtBearer pipeline are actually using the SAME
    // signing key: a token signed with a DIFFERENT key must be rejected outright.
    [Fact]
    public async Task Token_signed_with_the_wrong_key_is_rejected_with_401()
    {
        var client = _factory.CreateClient();
        var forgedToken = TestJwtFactory.CreateTokenWithWrongSigningKey(Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);

        var response = await client.GetAsync("/api/v1/admin/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task No_token_returns_401_not_403()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // §5.4: correct authentication, wrong role -> 403, never 401. This is the exact
    // distinction the reported 103-failure cascade got wrong (everything came back 401).
    [Fact]
    public async Task Valid_token_with_wrong_role_returns_403_not_401()
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.GetAsync("/api/v1/admin/customers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Confirms the fix directly: JwtSettings resolved from the built host's DI container
    // reflects the test factory's injected values, not appsettings.json's blank ones - this is
    // exactly the resolution path Program.cs's JwtBearerOptions post-configure callback uses.
    [Fact]
    public void JwtSettings_resolved_from_the_built_host_reflects_the_test_factory_override()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        Assert.Equal("rburger-tests", jwtSettings.Issuer);
        Assert.False(string.IsNullOrEmpty(jwtSettings.Key));
    }
}
