using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Infrastructure.Storage;

// Day 12 addition (Blocking Issue #3 - approved scaffold-only decision, same shape as
// NotConfiguredMenuItemImageStorage from Day 10). Documentation v1.2's §9.2 describes
// IPaymentProvider's contract and names Paymob/Fawry as the intended concrete providers, but
// never specifies the API key, merchant id, webhook secret, or any other credential needed to
// actually call a gateway. Registering this class satisfies DI (ChargePaymentCommandHandler,
// PaymentWebhookCommandHandler, and DeleteAdminOrderCommandHandler's card-refund branch can all
// be fully constructed and unit-tested against the IPaymentProvider abstraction) while being
// explicit and honest that no real gateway call can happen yet.
//
// DO NOT replace this with a fake/no-op "success" implementation - that would let the API
// silently claim a charge session was created or a refund succeeded when it wasn't, which is
// worse than a clear failure (this is the exact same reasoning already documented on
// NotConfiguredMenuItemImageStorage). When gateway configuration is actually provided, replace
// this class's registration in ServiceCollectionExtensions with a real Paymob/Fawry-backed
// implementation; no other code needs to change, since every consumer depends only on the
// IPaymentProvider interface.
public class NotConfiguredPaymentProvider : IPaymentProvider
{
    public Task<PaymentSession> CreateSessionAsync(Guid orderId, decimal amount, string currency)
    {
        throw new PaymentProviderNotConfiguredException();
    }

    public Task<RefundResult> RefundAsync(Guid paymentId, decimal amount)
    {
        throw new PaymentProviderNotConfiguredException();
    }

    public bool ValidateWebhookSignature(string payload, string signatureHeader)
    {
        // Deliberately throws rather than returning false: a plain "false" would be
        // indistinguishable from a legitimately-configured provider rejecting a forged
        // signature (§9.5: "rejects unsigned/invalid requests with 400"), silently misleading
        // the webhook handler into treating "not configured" as "signature check failed."
        throw new PaymentProviderNotConfiguredException();
    }
}
