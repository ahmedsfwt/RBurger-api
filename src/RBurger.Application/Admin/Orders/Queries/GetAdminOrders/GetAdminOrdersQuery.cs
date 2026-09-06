using MediatR;
using RBurger.Application.Admin.Orders.DTOs;
using RBurger.Application.Common.Models;
using RBurger.Domain.Enums;

namespace RBurger.Application.Admin.Orders.Queries.GetAdminOrders;

// §7.6.5: "paginated, filterable by branchId/stage/date range." Query params mirror §7.0's
// pagination convention (page default 1, pageSize default 20 - defaulting happens in the
// controller, same as every other paginated Admin endpoint) plus the four optional filters.
public record GetAdminOrdersQuery(
    int Page,
    int PageSize,
    int? BranchId,
    OrderStage? Stage,
    DateTime? DateFrom,
    DateTime? DateTo) : IRequest<PagedResponse<AdminOrderListItemDto>>;
