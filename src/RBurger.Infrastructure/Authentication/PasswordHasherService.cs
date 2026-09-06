using Microsoft.AspNetCore.Identity;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Authentication;

// Approved decision #3: uses only PasswordHasher<Customer> (PBKDF2-based, IdentityV3 by
// default). The `user` parameter of HashPassword/VerifyHashedPassword is unused by the default
// implementation - passing null! is safe here and avoids requiring a full IUserStore/UserManager.
public class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<Customer> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
