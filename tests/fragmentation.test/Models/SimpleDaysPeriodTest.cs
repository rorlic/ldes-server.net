using System.Globalization;
using LdesServer.Fragmentation.Models;

namespace LdesServer.Fragmentation.Test.Models;

public class SimpleDaysPeriodTest
{
    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-08-09T00:00:00Z", "2025-08-10T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 2, "2025-08-09T00:00:00Z", "2025-08-11T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 3, "2025-08-07T00:00:00Z", "2025-08-10T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 4, "2025-08-09T00:00:00Z", "2025-08-13T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 7, "2025-08-08T00:00:00Z", "2025-08-15T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 9, "2025-08-01T00:00:00Z", "2025-08-10T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 15, "2025-08-01T00:00:00Z", "2025-08-16T00:00:00Z")]
    [InlineData("2025-08-22T18:05:09Z", 15, "2025-08-16T00:00:00Z", "2025-08-31T00:00:00Z")]
    [InlineData("2025-08-22T18:05:09Z", 7, "2025-08-22T00:00:00Z", "2025-08-29T00:00:00Z")]
    public void CalculateDaysPeriodBucket(string timestamp, int days, string expectedFrom, string expectedTo)
    {
        var period = new SimpleDaysPeriod(days);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }

}