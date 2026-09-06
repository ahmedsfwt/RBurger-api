using System.Text.Json;
using MediatR;
using RBurger.Application.Common.Interfaces;
using RBurger.Application.Orders.DTOs;

namespace RBurger.Application.Orders.Commands.CreateOrder;

// §7.4 POST /api/v1/orders request body - fields match exactly:
// { branchId, items[], customerName, customerPhone, deliveryAddress, notes, paymentMethod }
public class CreateOrderCommand : IRequest<CreateOrderResponse>, IIdempotentRequest
{
    public int BranchId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    // Not part of the documented JSON body - populated by OrdersController from the
    // authenticated JWT's "sub" claim (§5.4), the same pattern GetCustomerMeQuery uses.
    // [JSON-bound requests simply leave this at its default; the controller overwrites it
    // after model binding, before dispatching to MediatR.]
    public Guid CustomerId { get; set; }

    // Not part of the documented JSON body - populated by OrdersController from the
    // "Idempotency-Key" request header (§5.5, §7.4). Day 13: now actually deduplicated by
    // IdempotencyBehavior (Common/Behaviors), persisted via IIdempotencyRepository.
    public string? IdempotencyKey { get; set; }

    // ---- IIdempotentRequest (Day 13 addition) ----
    string IIdempotentRequest.IdempotencyEndpoint => "orders:create";
    Guid IIdempotentRequest.IdempotencyScopeId => CustomerId;

    string IIdempotentRequest.IdempotencyFingerprint => JsonSerializer.Serialize(new
    {
        BranchId,
        Items,
        CustomerName,
        CustomerPhone,
        DeliveryAddress,
        Notes,
        PaymentMethod,
        CustomerId
    });
}
