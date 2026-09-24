using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using RBurger.Domain.Entities;

namespace RBurger.Infrastructure.Payments;

public class PaymobPaymentProvider : IPaymentProvider
{
    // Paymob HMAC: the order of these 20 fields is fixed by Paymob. All are read from "obj".
    private static readonly string[] HmacFieldPaths =
    {
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
        "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
        "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
        "source_data.pan", "source_data.sub_type", "source_data.type", "success"
    };

    private readonly HttpClient _httpClient;
    private readonly PaymobSettings _settings;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymobPaymentProvider> _logger;

    public PaymobPaymentProvider(
        HttpClient httpClient,
        IOptions<PaymobSettings> settings,
        IOrderRepository orderRepository,
        ILogger<PaymobPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<PaymentSession> CreateSessionAsync(Guid orderId, decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.IntegrationId <= 0)
        {
            throw new PaymentProviderNotConfiguredException();
        }

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId, CancellationToken.None);
        if (order is null)
        {
            throw new NotFoundException($"Order {orderId} was not found.");
        }

        var amountCents = ToCents(amount);
        var (firstName, lastName) = SplitName(order.CustomerName);

        var payload = new Dictionary<string, object?>
        {
            ["amount"] = amountCents,
            ["currency"] = currency,
            ["payment_methods"] = new[] { _settings.IntegrationId },
            ["items"] = BuildItems(order, amountCents),
            ["billing_data"] = new Dictionary<string, string>
            {
                ["first_name"] = firstName,
                ["last_name"] = lastName,
                ["phone_number"] = order.Phone,
                ["email"] = "customer@rburger.app",
                ["apartment"] = "NA",
                ["floor"] = "NA",
                ["building"] = "NA",
                ["street"] = "NA",
                ["city"] = "NA",
                ["state"] = "NA",
                ["country"] = "EG",
                ["postal_code"] = "NA",
                ["shipping_method"] = "NA"
            },
            // Comes back in the webhook as obj.order.merchant_order_id - this is how the
            // webhook finds our order.
            ["special_reference"] = order.Id.ToString()
        };

        if (!string.IsNullOrWhiteSpace(_settings.NotificationUrl))
        {
            payload["notification_url"] = _settings.NotificationUrl;
        }

        var url = new Uri($"{_settings.BaseUrl.TrimEnd('/')}/v1/intention/");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _settings.SecretKey);

        int statusCode;
        string body;
        try
        {
            using var response = await _httpClient.SendAsync(request);
            statusCode = (int)response.StatusCode;
            body = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Paymob request failed for order {OrderId}.", orderId);
            throw new PaymentGatewayException("Could not reach Paymob.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Paymob request timed out for order {OrderId}.", orderId);
            throw new PaymentGatewayException("Paymob request timed out.", ex);
        }

        if (statusCode is < 200 or >= 300)
        {
            _logger.LogError(
                "Paymob create-intention failed for order {OrderId}. Status {Status}. Body: {Body}",
                orderId, statusCode, body);
            throw new PaymentGatewayException("Paymob rejected the payment intention request.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var clientSecret = root.TryGetProperty("client_secret", out var secretElement)
                               && secretElement.ValueKind == JsonValueKind.String
                ? secretElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new PaymentGatewayException("Paymob did not return a client_secret.");
            }

            var intentionId = root.TryGetProperty("id", out var idElement)
                ? (idElement.ValueKind == JsonValueKind.String ? idElement.GetString() : idElement.GetRawText())
                : null;

            // RedirectUrl stays empty: the Flutter SDK only needs clientSecret (the frontend
            // holds the public key).
            return new PaymentSession(intentionId ?? string.Empty, string.Empty, clientSecret);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Paymob returned an unreadable response for order {OrderId}.", orderId);
            throw new PaymentGatewayException("Paymob returned an unreadable response.", ex);
        }
    }

    // Refunds are a separate task - keep today's behavior (503) so DeleteAdminOrder never
    // cancels a captured card order without a real refund.
    public Task<RefundResult> RefundAsync(Guid paymentId, decimal amount)
        => throw new PaymentProviderNotConfiguredException();

    public bool ValidateWebhookSignature(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.HmacSecret))
        {
            throw new PaymentProviderNotConfiguredException();
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("obj", out var obj)
                || obj.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var concatenated = string.Concat(HmacFieldPaths.Select(path => Stringify(obj, path)));

            var hash = HMACSHA512.HashData(
                Encoding.UTF8.GetBytes(_settings.HmacSecret),
                Encoding.UTF8.GetBytes(concatenated));

            var expected = Convert.ToHexString(hash).ToLowerInvariant();
            var received = signatureHeader.Trim().ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(received));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // ---- helpers ----

    // Paymob's string format: true/false lowercase, numbers as raw text, null -> "".
    // created_at MUST stay a string exactly as received.
    private static string Stringify(JsonElement obj, string path)
    {
        var current = obj;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out var next))
            {
                return string.Empty;
            }

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.String => current.GetString() ?? string.Empty,
            _ => current.GetRawText()
        };
    }

    private static long ToCents(decimal value)
        => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);

    // Paymob requires sum(items.amount) == amount. Each line uses quantity = 1 with
    // amount = unitPrice * quantity, to avoid any ambiguity about how Paymob multiplies.
    // If the sum ever differs, fall back to a single line with the exact total.
    private static List<Dictionary<string, object>> BuildItems(Order order, long amountCents)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var orderItem in order.OrderItems)
        {
            var name = string.IsNullOrWhiteSpace(orderItem.NameEn) ? orderItem.NameAr : orderItem.NameEn;
            items.Add(Item(name, ToCents(orderItem.UnitPrice * orderItem.Quantity), $"x{orderItem.Quantity}"));
        }

        if (order.DeliveryFee > 0)
        {
            items.Add(Item("Delivery fee", ToCents(order.DeliveryFee), "Delivery"));
        }

        var sum = items.Sum(i => (long)i["amount"]);
        if (items.Count == 0 || sum != amountCents)
        {
            return new List<Dictionary<string, object>>
            {
                Item($"R Burger order #{order.OrderNumber}", amountCents, "Order")
            };
        }

        return items;
    }

    private static Dictionary<string, object> Item(string name, long cents, string description) => new()
    {
        ["name"] = Truncate(name, 50),
        ["amount"] = cents,
        ["description"] = Truncate(description, 100),
        ["quantity"] = 1
    };

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
        {
            return value;
        }

        var length = char.IsHighSurrogate(value[max - 1]) ? max - 1 : max;
        return value[..length];
    }

    private static (string First, string Last) SplitName(string? fullName)
    {
        var parts = (fullName ?? string.Empty).Trim().Split(
            ' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return (parts.Length > 0 ? parts[0] : "NA", parts.Length > 1 ? parts[1] : "NA");
    }
}