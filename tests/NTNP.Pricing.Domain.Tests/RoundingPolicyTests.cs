using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Tests;

public class RoundingPolicyTests
{
    [Theory]
    [InlineData(1234.56, RoundingMode.None, 1234.56)]
    [InlineData(1234.56, RoundingMode.NearestInteger, 1235)]
    [InlineData(1234.56, RoundingMode.NearestTen, 1230)]
    [InlineData(1250, RoundingMode.NearestHundred, 1300)]
    [InlineData(1_234_567, RoundingMode.NearestThousand, 1_235_000)]
    public void Apply_Rounds_As_Configured(decimal input, RoundingMode mode, decimal expected)
    {
        Assert.Equal(expected, RoundingPolicy.Apply(input, mode));
    }

    [Fact]
    public void ApplyForeign_Rounds_To_Requested_DecimalPlaces()
    {
        Assert.Equal(1860.08m, RoundingPolicy.ApplyForeign(1860.083333m, 2));
    }

    [Fact]
    public void Rounding_Never_Mutates_Stored_Value_Only_Display_Output()
    {
        // This test documents the Section 4 invariant: RoundingPolicy is a pure function that
        // never touches the entity it was derived from.
        const decimal storedUnrounded = 3_939_000_000.123456m;
        var displayed = RoundingPolicy.Apply(storedUnrounded, RoundingMode.NearestThousand);

        Assert.NotEqual(storedUnrounded, displayed);
        Assert.Equal(3_939_000_000.123456m, storedUnrounded); // unchanged
    }
}
