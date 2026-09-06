using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Api.Common;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CustomerId => GetIdForRole("Customer");

    // Day 7 addition: mirrors CustomerId exactly, scoped to role=Driver. A role check is now
    // required on both getters (not just relying on the controller's [Authorize(Roles=...)])
    // because GetOrderById (§7.4) and the Day 7 driver endpoints can both be reached with
    // either a Customer or a Driver JWT depending on the route - without the role check here,
    // a Driver JWT's "sub" claim would also be misread as a non-null CustomerId (and vice
    // versa), since both claim types share the same "sub" claim name.
    public Guid? DriverId => GetIdForRole("Driver");

    // Day 10 addition: mirrors CustomerId/DriverId exactly, scoped to role=Admin.
    public Guid? AdminId => GetIdForRole("Admin");

    private Guid? GetIdForRole(string expectedRole)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        // MapInboundClaims = false is set in Program.cs's JwtBearer options, so the claim
        // type here is the literal "role"/"sub" written by JwtTokenGenerator, not a remapped URI.
        var role = user?.FindFirst("role")?.Value;
        if (role != expectedRole)
        {
            return null;
        }

        var value = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
