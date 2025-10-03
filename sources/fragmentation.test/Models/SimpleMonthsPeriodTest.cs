using System.Globalization;
using AquilaSolutions.LdesServer.Fragmentation.Models;

namespace AquilaSolutions.LdesServer.Fragmentation.Test.Models;

public class SimpleMonthsPeriodTest
{
    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-08-01T00:00:00Z", "2025-09-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 2, "2025-07-01T00:00:00Z", "2025-09-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 3, "2025-07-01T00:00:00Z", "2025-10-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 4, "2025-05-01T00:00:00Z", "2025-09-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 6, "2025-07-01T00:00:00Z", "2026-01-01T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 9, "2025-01-01T00:00:00Z", "2025-10-01T00:00:00Z")]
    [InlineData("2025-12-09T18:05:09Z", 1, "2025-12-01T00:00:00Z", "2026-01-01T00:00:00Z")]
    public void CalculateMonthsPeriodBucket(string timestamp, int months, string expectedFrom, string expectedTo)
    {
        var period = new SimpleMonthsPeriod(months);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }

}