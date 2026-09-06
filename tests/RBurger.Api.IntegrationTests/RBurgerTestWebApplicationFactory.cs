using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RBurger.Api.IntegrationTests;

// Day 16 addition. Program.cs now fails fast at startup if Jwt:Issuer/Audience/Key are
// missing (Backend Parity Spec §4 - removing the unsafe 32-zero fallback signing key). A bare
// WebApplicationFactory<Program> only picks up config from appsettings.json (blank, by
// design) plus whatever User Secrets/environment variables happen to be present on the
// machine running the tests - so integration tests would now fail to even start the host
// unless the developer happened to have real Jwt secrets configured locally.
//
// This factory injects deterministic, non-secret, test-only Jwt/RefreshToken values as the
// highest-priority configuration source (AddInMemoryCollection appended last always wins),
// so the test suite is self-contained and does not depend on any developer's local
// User Secrets. These values are never used outside the test host process.
public class RBurgerTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "rburger-tests",
                ["Jwt:Audience"] = "rburger-tests",
                ["Jwt:Key"] = "test-only-signing-key-not-a-real-secret-32chars",
                ["RefreshToken:ExpiresInDays"] = "30"
            });
        });

        base.ConfigureWebHost(builder);
    }
}
