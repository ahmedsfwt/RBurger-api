namespace RBurger.Domain.Enums;

// §1.3 / §6.2 / §12.1: Integer 0-3 lifecycle, values are binding and must not be re-declared elsewhere.
public enum OrderStage
{
    Confirmed = 0,
    Preparing = 1,
    OnTheWay = 2,
    Delivered = 3
}
