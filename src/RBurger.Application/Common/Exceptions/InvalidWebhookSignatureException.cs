namespace RBurger.Application.Common.Exceptions;

public class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException()
        : base("The webhook signature is missing or invalid.")
    {
    }
}