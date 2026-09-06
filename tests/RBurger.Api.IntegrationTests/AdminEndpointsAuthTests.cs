using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RBurger.Api.IntegrationTests;

// §5.4 / §7.6: "Every controller action is decorated with [Authorize(Roles="...")] matching
// this table exactly ... any attempt to call an /admin/* route with a Customer or Driver JWT"
// must be rejected. §13.4 requires this covered by automated integration tests. Following the
// exact same convention as DriverOrdersEndpointsAuthTests: these tests only exercise
// routing/authorization (independent of any database), since MediatR/repository access never
// runs for a request [Authorize] itself already rejects.
public class AdminEndpointsAuthTests : IClassFixture<RBurgerTestWebApplicationFactory>
{
    private readonly RBurgerTestWebApplicationFactory _factory;

    public AdminEndpointsAuthTests(RBurgerTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static readonly (string Method, string Url)[] AdminRoutes =
    {
        ("POST", "/api/v1/admin/menu-items"),
        ("PUT", "/api/v1/admin/menu-items/106"),
        ("DELETE", "/api/v1/admin/menu-items/106"),
        ("POST", "/api/v1/admin/menu-items/106/image"),
        ("DELETE", "/api/v1/admin/menu-items/106/image"),
        ("POST", "/api/v1/admin/branches"),
        ("PUT", "/api/v1/admin/branches/1"),
        ("DELETE", "/api/v1/admin/branches/1"),

        // Day 11 additions (§7.6.3 Driver Management + §7.6.4 Customer Management).
        ("POST", "/api/v1/admin/drivers"),
        ("GET", "/api/v1/admin/drivers"),
        ("PUT", "/api/v1/admin/drivers/00000000-0000-0000-0000-000000000001"),
        ("PATCH", "/api/v1/admin/drivers/00000000-0000-0000-0000-000000000001/status"),
        ("DELETE", "/api/v1/admin/drivers/00000000-0000-0000-0000-000000000001"),
        ("GET", "/api/v1/admin/customers"),
        ("DELETE", "/api/v1/admin/customers/00000000-0000-0000-0000-000000000001"),

        // Day 12 additions (§7.6.5 Order Monitoring + §7.6.6 Reviews & Analytics).
        ("GET", "/api/v1/admin/orders"),
        ("DELETE", "/api/v1/admin/orders/00000000-0000-0000-0000-000000000001"),
        ("GET", "/api/v1/admin/reviews"),
        ("DELETE", "/api/v1/admin/reviews/00000000-0000-0000-0000-000000000001"),
        ("GET", "/api/v1/admin/analytics/overview?range=7d"),
        ("GET", "/api/v1/admin/analytics/revenue-trend?days=7"),
        ("GET", "/api/v1/admin/analytics/orders-by-status"),
        ("GET", "/api/v1/admin/analytics/orders-by-branch"),
        ("GET", "/api/v1/admin/analytics/top-items?limit=5"),
        ("GET", "/api/v1/admin/analytics/driver-performance"),
        ("GET", "/api/v1/admin/analytics/rating-distribution"),

        // Day 14 additions (Backend Parity Spec §2 - toggle endpoints).
        ("PATCH", "/api/v1/admin/branches/1/toggle-status"),
        ("PATCH", "/api/v1/admin/menu-items/106/toggle-availability"),
        ("PATCH", "/api/v1/admin/drivers/00000000-0000-0000-0000-000000000001/toggle-status")
    };

    public static IEnumerable<object[]> AdminRoutesData => AdminRoutes.Select(r => new object[] { r.Method, r.Url });

    [Theory]
    [MemberData(nameof(AdminRoutesData))]
    public async Task Admin_routes_without_token_return_401(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminRoutesData))]
    public async Task Admin_routes_with_Customer_JWT_return_403(string method, string url)
    {
        var client = _factory.CreateClient();
        var customerToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §5.4: "a Driver JWT can never call a Customer- or Admin-only endpoint and vice versa".
    [Theory]
    [MemberData(nameof(AdminRoutesData))]
    public async Task Admin_routes_with_Driver_JWT_return_403(string method, string url)
    {
        var client = _factory.CreateClient();
        var driverToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Driver");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §13.4: "invalid JWT" - a token signed with the wrong key must fail signature validation
    // (401), before role authorization ever runs.
    [Fact]
    public async Task Admin_route_with_invalid_signature_token_returns_401()
    {
        var client = _factory.CreateClient();
        var invalidToken = TestJwtFactory.CreateTokenWithWrongSigningKey(Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        var response = await client.PostAsync("/api/v1/admin/branches", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Proves an Admin JWT is NOT rejected by role authorization itself (the opposite direction
    // of the 403 tests above) - mirrors Get_OrderById_with_Driver_JWT_is_not_rejected_by_role_authorization's
    // convention in DriverOrdersEndpointsAuthTests. Without a database, the actual outcome is
    // expected to be something other than 401/403 (a 404/422/500 from the handler reaching for
    // data that isn't there), which is out of scope for this routing-only test.
    [Theory]
    [MemberData(nameof(AdminRoutesData))]
    public async Task Admin_routes_with_Admin_JWT_are_not_rejected_by_role_authorization(string method, string url)
    {
        var client = _factory.CreateClient();
        var adminToken = TestJwtFactory.CreateToken(_factory, Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // §7.6.0: "Admin accounts have no signup endpoint either ... Only login is public." No
    // equivalent of DriverAuthEndpointsTests' 404-signup-route test is needed here since no
    // admin/signup route was ever added in the first place (nothing to assert an absence of
    // beyond what routing itself already proves - there is no [HttpPost("admin/signup")]
    // anywhere in AuthController).
    [Fact]
    public async Task Post_AdminLogin_with_empty_body_returns_400_validation_error()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/admin/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // §7.6.0 login route itself must be public/unauthenticated-reachable. Deliberately NOT
    // asserted via "response is not 401", unlike an initial version of this test: with a real
    // database attached, valid-shaped-but-wrong admin credentials also produce 401
    // (InvalidCredentialsException, mapped by GlobalExceptionHandler - the same pre-existing
    // convention already used for Customer/Driver login), which is indistinguishable by status
    // code alone from a 401 raised by the JWT-authorization middleware for a missing token.
    // That made the removed assertion unsound rather than a real product bug. Mirrors
    // DriverAuthEndpointsTests, which never attempts this same check for the equivalent public
    // Driver login route - only the 400-empty-body case (above) is asserted here for the same
    // reason: it's the one outcome that's unambiguous regardless of database state, since
    // FluentValidation's ValidationBehavior runs before any repository/DB access.
}
