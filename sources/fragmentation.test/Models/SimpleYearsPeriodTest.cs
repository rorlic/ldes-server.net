using System.Globalization;
using AquilaSolutions.LdesServer.Fragmentation.Models;

namespace AquilaSolutions.LdesServer.Fragmentation.Test.Models;

public class SimpleYearsPeriodTest
{

    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-01-01T00:00:00Z", "2026-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 5, "2025-01-01T00:00:00Z", "2030-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 10, "2020-01-01T00:00:00Z", "2030-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 25, "2025-01-01T00:00:00Z", "2050-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 50, "2000-01-01T00:00:00Z", "2050-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 100, "2000-01-01T00:00:00Z", "2100-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 200, "2000-01-01T00:00:00Z", "2200-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 500, "2000-01-01T00:00:00Z", "2500-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 2500, "0000-01-01T00:00:00Z", "2500-01-01T00:00:00Z")]
    public void CalculateYearsPeriodBucket(string timestamp, int years, string expectedFrom, string expectedTo)

    {
        var period = new SimpleYearsPeriod(years);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }
}