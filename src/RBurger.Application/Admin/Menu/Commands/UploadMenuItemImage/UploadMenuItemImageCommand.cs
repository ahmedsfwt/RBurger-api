using MediatR;
using RBurger.Application.Admin.Menu.DTOs;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Admin.Menu.Commands.UploadMenuItemImage;

// §7.6.1 POST /api/v1/admin/menu-items/{id}/image - multipart/form-data, Idempotency-Key
// header required. Content is carried as a raw Stream + declared content-type/length rather
// than Microsoft.AspNetCore.Http.IFormFile, so Application stays free of ASP.NET Core HTTP
// types (the controller is responsible for extracting these from the multipart request).
// Day 13: implements IIdempotentRequest - now actually deduplicated (see IdempotencyBehavior).
public class UploadMenuItemImageCommand : IRequest<MenuItemImageUploadResponse>, IIdempotentRequest
{
    public int MenuItemId { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string? IdempotencyKey { get; set; }

    // Not part of the documented multipart request - populated by AdminMenuItemsController
    // from the authenticated Admin JWT's "sub" claim, mirroring CreateOrderCommand.CustomerId's
    // pattern, so this endpoint's idempotency scope is per-Admin (Day 13 addition).
    public Guid AdminId { get; set; }

    // ---- IIdempotentRequest (Day 13 addition) ----
    string IIdempotentRequest.IdempotencyEndpoint => "menu-items:upload-image";
    Guid IIdempotentRequest.IdempotencyScopeId => AdminId;

    // Content is a raw upload Stream - not hashed byte-for-byte (unnecessary I/O for a
    // multi-megabyte file on every request); content-type + declared length serve as a proxy
    // for "the same file", consistent with what UploadMenuItemImageCommandValidator already
    // validates against.
    string IIdempotentRequest.IdempotencyFingerprint => $"{MenuItemId}|{ContentType}|{ContentLength}|{AdminId}";
}
