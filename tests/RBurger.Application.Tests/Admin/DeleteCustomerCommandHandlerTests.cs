using RBurger.Application.Admin.Customers.Commands.DeleteCustomer;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Tests.Authentication;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class DeleteCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_throws_NotFoundException_when_customer_does_not_exist()
    {
        var customers = new FakeCustomerRepository();
        var handler = new DeleteCustomerCommandHandler(customers);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteCustomerCommand { Id = Guid.NewGuid() }, default));
    }

    [Fact]
    public async Task Handle_removes_the_customer_when_it_exists()
    {
        var customers = new FakeCustomerRepository();
        var customer = new Customer { Id = Guid.NewGuid(), FullName = "Ahmed Sami", Phone = "01012345678" };
        customers.Customers.Add(customer);
        var handler = new DeleteCustomerCommandHandler(customers);

        await handler.Handle(new DeleteCustomerCommand { Id = customer.Id }, default);

        Assert.Empty(customers.Customers);
    }

    // §7.6.4: "Order history rows are retained ... but CustomerId is nulled." This behavior is
    // implemented via CustomerConfiguration's OnDelete(DeleteBehavior.SetNull) (Day 2 approved
    // decision) - a database-engine cascade that fires as part of the same DELETE
    // statement/transaction when the real EF Core CustomerRepository issues it, which is what
    // gives the delete-customer + anonymize-orders operation its atomicity (single DB
    // round-trip, no separate Order-side write needed).
    //
    // This cascade is NOT exercisable through RBurger.Application.Tests' dependency-free,
    // no-database fakes (FakeCustomerRepository.DeleteAsync only removes from its own in-memory
    // list - see its XML comment), and the current RBurger.Api.IntegrationTests project has no
    // database configured either (WebApplicationFactory<Program> talks to whatever connection
    // string Program.cs resolves, but every existing integration test - including
    // AdminEndpointsAuthTests - is deliberately scoped to routing/authorization only, never
    // exercising a real repository call). Asserting "Orders retained + CustomerId nulled" here
    // would therefore either be a no-op against the fake (proving nothing) or require standing
    // up a real SQL Server instance, which is out of scope for this test project as currently
    // configured.
    //
    // This is the exact same honest limitation already accepted in Day 10 for
    // FakeBranchRepository.SaveChangesAsync's FK-Restrict scenario (see its XML comment) -
    // flagged here rather than papering over it with a test that doesn't actually prove the
    // documented behavior. See the Day 11 report's Deferred Items / Remaining Verification
    // section: this specific cascade should be exercised once a real-database integration test
    // harness exists.
    [Fact]
    public void Cascade_nulling_of_Order_CustomerId_is_a_DB_level_behavior_not_unit_testable_here()
    {
        // Intentionally empty - documents the limitation above as an explicit, discoverable
        // test rather than a silent gap. See CustomerConfiguration (Infrastructure) for the
        // actual OnDelete(DeleteBehavior.SetNull) configuration this relies on.
        Assert.True(true);
    }
}
