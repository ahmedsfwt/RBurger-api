namespace RBurger.Application.Common.Interfaces;

// Day 10 approved decision (Blocking Issue #2): §7.6.1 documents that menu-item photos are
// stored in S3 behind a CloudFront URL, but the Technical & Product Documentation v1.2 never
// specifies the concrete provider configuration (bucket name, region, credentials, CDN
// domain). This abstraction is therefore SCAFFOLDED ONLY for Day 10 - it defines the exact
// contract §7.6.1 needs (validated content-type/size in, ImageUrl/ImageObjectKey out) so the
// Application-layer commands/handlers/validators can be fully implemented and tested now,
// without inventing any AWS configuration.
//
// The concrete Infrastructure implementation registered for Day 10
// (NotConfiguredMenuItemImageStorage) intentionally always throws StorageNotConfiguredException
// - see that class's XML comment. A real AWSSDK.S3-backed implementation is deferred to a
// future day once bucket/region/credentials/CDN domain are actually specified.
public interface IMenuItemImageStorage
{
    // Uploads (or replaces) the photo for the given menu item. If existingObjectKeyToReplace
    // is not null, the implementation is responsible for deleting that previous object after
    // the new one is written successfully (§7.6.1: "deletes the previous object if one
    // existed"). Returns the new ImageUrl (CloudFront-fronted, §6.2/§10.1) and ImageObjectKey
    // (the S3 object key, §6.2) for the caller to persist.
    Task<MenuItemImageUploadResult> UploadAsync(
        int menuItemId,
        Stream content,
        string contentType,
        string? existingObjectKeyToReplace,
        CancellationToken cancellationToken);

    // Deletes the S3 object behind the given ImageObjectKey (§7.6.1's image-delete endpoint).
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

// §7.6.1 image-upload response shape's two server-written fields (imageUrl, imageUploadedAt is
// stamped by the handler itself using the same GETUTCDATE()-equivalent convention used
// elsewhere in this codebase, not by the storage implementation).
public record MenuItemImageUploadResult(string ImageUrl, string ImageObjectKey);
