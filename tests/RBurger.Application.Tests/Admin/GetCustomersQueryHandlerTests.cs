using RBurger.Application.Admin.Customers.Queries.GetCustomers;
using RBurger.Application.Tests.Authentication;
using RBurger.Application.Tests.Orders;
using RBurger.Domain.Entities;
using Xunit;

namespace RBurger.Application.Tests.Admin;

public class GetCustomersQueryHandlerTests
{
    private static (GetCustomersQueryHandler Handler, FakeCustomerRepository Customers, FakeOrderRepository Orders)
        BuildHandler()
    {
        var customers = new FakeCustomerRepository();
        var orders = new FakeOrderRepository();
        var handler = new GetCustomersQueryHandler(customers, orders);
        return (handler, customers, orders);
    }

    [Fact]
    public async Task Handle_paginates_per_documented_defaults()
    {
        var (handler, customers, _) = BuildHandler();
        for (var i = 0; i < 25; i++)
        {
            customers.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                FullName = $"Customer {i}",
                Phone = $"0101234{i:D4}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        var result = await handler.Handle(new GetCustomersQuery(), default);

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Handle_computes_lifetime_orders_count()
    {
        var (handler, customers, orders) = BuildHandler();
        var customer = new Customer { Id = Guid.NewGuid(), FullName = "Ahmed Sami", Phone = "01012345678" };
        customers.Customers.Add(customer);

        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), CustomerId = customer.Id });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), CustomerId = customer.Id });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), CustomerId = customer.Id });
        orders.AddedOrders.Add(new Order { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid() }); // another customer

        var result = await handler.Handle(new GetCustomersQuery(), default);

        Assert.Equal(3, result.Items.Single().OrdersCount);
    }

    [Fact]
    public async Task Handle_returns_zero_orders_count_for_a_customer_with_no_orders()
    {
        var (handler, customers, _) = BuildHandler();
        customers.Customers.Add(new Customer { Id = Guid.NewGuid(), FullName = "Ahmed Sami", Phone = "01012345678" });

        var result = await handler.Handle(new GetCustomersQuery(), default);

        Assert.Equal(0, result.Items.Single().OrdersCount);
    }
}
