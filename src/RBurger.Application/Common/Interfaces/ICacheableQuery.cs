namespace RBurger.Application.Common.Interfaces;

// Day 15 addition (Backend Parity Spec §12). Implemented by the 7 documented analytics queries
// only (§13.1's 60s caching requirement is explicitly scoped to "Admin analytics endpoints") -
// CachingBehavior (Common/Behaviors) wraps any MediatR request implementing this; everything
// else passes through unaffected.
public interface ICacheableQuery
{
    // Must uniquely identify this query's parameters (e.g. include the "range"/"days" value),
    // so two different parameterizations of the same query never collide in the cache.
    string CacheKey { get; }

    // §13.1: "cached server-side for 60 s" - kept as a per-query property rather than a single
    // hardcoded constant so an individual analytics endpoint could use a different documented
    // duration in the future without changing CachingBehavior itself.
    int CacheDurationSeconds { get; }
}
