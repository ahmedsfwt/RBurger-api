using RBurger.Application.Common.Models;
using RBurger.Domain.Entities;
// Day 10 fix - see IAdminRepository.cs's comment for the full explanation of why this alias
// is required (RBurger.Application.Admin namespace shadows the bare "Admin" type name).
using AdminEntity = RBurger.Domain.Entities.Admin;

namespace RBurger.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    // Token contains only the approved claims: sub = CustomerId, role = Customer (§5.4).
    // No additional claims are added per the approved Day 3 decision.
    JwtTokenResult GenerateAccessToken(Customer customer);

    // Day 6 addition: token contains only sub = DriverId, role = Driver (§5.4), mirroring the
    // Customer overload's minimalism exactly - approved Day 6 decision. No branchId claim is
    // embedded; any future Driver-scoped endpoint (§7.5) must resolve the driver's branch via a
    // server-side lookup by DriverId, never by trusting a token claim, consistent with the
    // "never trust the client/token for business data" principle already used for pricing (§7.4).
    JwtTokenResult GenerateAccessToken(Driver driver);

    // Day 10 addition: mirrors the Customer/Driver overloads exactly. Approved decision:
    // token contains only sub = AdminId, role = Admin (§5.4, §7.6.0) - no additional claims,
    // consistent with the "no invented claims" rule already applied to Customer/Driver.
    JwtTokenResult GenerateAccessToken(AdminEntity admin);

    // Day 13: refresh-token persistence is now implemented (approved schema change - see
    // RefreshToken.cs). Generates the raw, high-entropy opaque value plus its expiry
    // (RefreshTokenSettings.ExpiresInDays - see that class's XML comment for why the TTL
    // itself is undocumented configuration, mirroring JwtSettings' exact precedent). The
    // caller (each login/signup handler, and RefreshTokenCommandHandler) is responsible for
    // hashing the raw value (RefreshTokenHasher) and persisting the RefreshToken row via
    // IRefreshTokenRepository - this method itself has no DB dependency, consistent with
    // JwtTokenGenerator living in Infrastructure/Authentication, not Infrastructure/Persistence.
    RefreshTokenResult GenerateOpaqueRefreshToken();
}
