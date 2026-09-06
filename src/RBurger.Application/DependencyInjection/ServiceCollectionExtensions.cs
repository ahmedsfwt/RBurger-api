using System.Reflection;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RBurger.Application.Authentication.DTOs;
using RBurger.Application.Common.Behaviors;
using RBurger.Domain.Entities;

namespace RBurger.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static readonly Assembly ThisAssembly = typeof(ServiceCollectionExtensions).Assembly;

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ThisAssembly));
        services.AddValidatorsFromAssembly(ThisAssembly);

        // Registration order = pipeline execution order. ValidationBehavior must run before
        // IdempotencyBehavior so the Idempotency-Key NotEmpty rule (§5.5) rejects a
        // missing/blank key before any database work happens.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Day 14 addition: database-backed idempotency for the three documented
        // Idempotency-Key-required endpoints (§5.5) - see IdempotencyBehavior's XML comment.
        // A no-op for every other request (anything not implementing IIdempotentRequest).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        // Day 15 addition (Backend Parity Spec §12): 60s server-side caching, scoped to the
        // analytics queries that implement ICacheableQuery - see CachingBehavior's XML comment.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        ConfigureMapster();

        return services;
    }

    private static void ConfigureMapster()
    {
        // §7.1: GET /customers/me response has "customerId" (maps from Customer.Id) and
        // "address" (maps from Customer.DefaultAddress) - property names differ from the entity,
        // so an explicit mapping is required per the "Use Mapster where mapping is required" rule.
        TypeAdapterConfig<Customer, CustomerMeResponse>.NewConfig()
            .Map(dest => dest.CustomerId, src => src.Id)
            .Map(dest => dest.Address, src => src.DefaultAddress);
    }
}
