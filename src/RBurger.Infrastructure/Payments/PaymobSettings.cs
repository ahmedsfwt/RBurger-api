namespace RBurger.Infrastructure.Payments;

public class PaymobSettings
{
    public const string SectionName = "Paymob";

    public string BaseUrl { get; set; } = "https://accept.paymob.com";
    public string SecretKey { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;
    public int IntegrationId { get; set; }
    public string NotificationUrl { get; set; } = string.Empty;
}