namespace RBurger.Application.Common.Interfaces;

// Narrow interface per approved decision #3: wraps Microsoft.AspNetCore.Identity's
// PasswordHasher<Customer> only. No UserManager/SignInManager/IUserStore/lockout/2FA.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}
