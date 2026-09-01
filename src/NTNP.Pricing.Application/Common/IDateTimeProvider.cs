namespace NTNP.Pricing.Application.Common;

/// <summary>Testability seam for "now" (Section: deterministic tests for effective-dated data).</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
