using MediatR;
using RBurger.Application.Common.Interfaces;

namespace RBurger.Application.Common.Behaviors;

// Day 15 addition (Backend Parity Spec §12) - see ICacheableQuery's XML comment. A no-op for
// any request that doesn't implement ICacheableQuery.
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cacheService;

    public CachingBehavior(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        var cached = await _cacheService.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = await next();

        await _cacheService.SetAsync(
            cacheable.CacheKey, response, TimeSpan.FromSeconds(cacheable.CacheDurationSeconds), cancellationToken);

        return response;
    }
}
