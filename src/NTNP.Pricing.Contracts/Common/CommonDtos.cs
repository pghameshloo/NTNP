namespace NTNP.Pricing.Contracts.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record PagedQuery(string? Search = null, int Page = 1, int PageSize = 25, string? SortBy = null, bool SortDescending = false);

/// <summary>Uniform error payload for API responses (validation, domain, not-found, concurrency).</summary>
public sealed record ApiErrorResponse(string Type, string Title, int Status, IReadOnlyList<string> Errors, string? TraceId = null);

/// <summary>Returned on a 409 concurrency conflict so the client can show "what changed" (Section 31).</summary>
public sealed record ConcurrencyConflictResponse(string EntityType, string EntityId, string Message);

public sealed record ServerStatusDto(
    string ApiVersion,
    string DatabaseSchemaVersion,
    bool DatabaseReachable,
    DateTimeOffset ServerTimeUtc);
