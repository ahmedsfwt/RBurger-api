namespace RBurger.Application.Common.Exceptions;

// Day 10 addition (Blocking Issue #2, approved scaffold-only decision). Thrown by
// NotConfiguredMenuItemImageStorage when the menu-photo upload/delete endpoints (§7.6.1) are
// actually invoked, since no concrete S3/AWS provider configuration exists yet in
// Documentation v1.2 or in this codebase. NOT a documented §7.8 status code - this is an
// honest "feature not yet deployed" signal (503), not a business-rule violation (422) or a
// client error, so it is deliberately kept distinct from every other mapped exception here.
public class StorageNotConfiguredException : Exception
{
    public StorageNotConfiguredException()
        : base("Menu item image storage is not yet configured in this environment.")
    {
    }
}
