using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Infrastructure.Storage;

// Day 10 approved decision (Blocking Issue #2 - scaffold-only approach). Documentation v1.2's
// §7.6.1/§10.1 describe menu-item photos as stored in S3 behind a CloudFront URL, but never
// specify the concrete bucket name, region, credentials, or CDN domain needed to actually
// wire up AWSSDK.S3. Registering this class satisfies DI (UploadMenuItemImageCommandHandler
// and DeleteMenuItemImageCommandHandler can be fully constructed and unit-tested against the
// IMenuItemImageStorage abstraction) while being explicit and honest that no real upload/
// delete can happen yet.
//
// DO NOT replace this with a fake/no-op "success" implementation - that would let the API
// silently claim an image was stored when it wasn't, which is worse than a clear failure.
// When AWS S3 configuration is actually provided (bucket, region, credentials, CDN domain),
// replace this class's registration in ServiceCollectionExtensions with a real
// AWSSDK.S3-backed implementation; no other code in this feature needs to change, since both
// command handlers already depend only on the IMenuItemImageStorage interface.
public class NotConfiguredMenuItemImageStorage : IMenuItemImageStorage
{
    public Task<MenuItemImageUploadResult> UploadAsync(
        int menuItemId,
        Stream content,
        string contentType,
        string? existingObjectKeyToReplace,
        CancellationToken cancellationToken)
    {
        throw new StorageNotConfiguredException();
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        throw new StorageNotConfiguredException();
    }
}
