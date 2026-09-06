using RBurger.Domain.Entities;
// Day 10 fix: RBurger.Application.Admin (the Menu/Branches feature namespace added this day)
// is a nested namespace directly under RBurger.Application. C#'s simple-name lookup checks
// enclosing namespaces for a matching nested namespace/type BEFORE it ever consults using
// directives, so bare "Admin" here would bind to that namespace, not the Domain.Entities.Admin
// entity, producing CS0118 ("'Admin' is a namespace but is used like a type"). Aliased instead
// of renaming the Domain entity or the feature namespace, per Ahmed's explicit instruction.
using AdminEntity = RBurger.Domain.Entities.Admin;

namespace RBurger.Application.Common.Interfaces;

// Day 10 addition, mirroring IDriverRepository/ICustomerRepository's minimal read-only shape.
// §7.6.0 scope is login-only ("Admin accounts have no signup endpoint either... Only login
// is public") - no AddAsync/SaveChangesAsync, since Admin provisioning is out-of-band
// (seed migration / internal-only endpoint not exposed to any client, §6.2/§7.6.0) and is
// explicitly out of scope for this API surface.
public interface IAdminRepository
{
    Task<AdminEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    // Day 13 addition: used by RefreshTokenCommandHandler to re-verify an Admin account is
    // still active (IsActive) before honoring a refresh - the same account-still-valid check
    // ICustomerRepository/IDriverRepository's existing GetByIdAsync already enables for the
    // Customer/Driver branches of that same flow.
    Task<AdminEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
