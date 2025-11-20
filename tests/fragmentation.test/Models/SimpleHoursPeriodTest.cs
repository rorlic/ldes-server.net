using System.Globalization;
using LdesServer.Fragmentation.Models;

namespace LdesServer.Fragmentation.Test.Models;

public class SimpleHoursPeriodTest
{
    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-08-09T18:00:00Z", "2025-08-09T19:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 2, "2025-08-09T18:00:00Z", "2025-08-09T20:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 3, "2025-08-09T18:00:00Z", "2025-08-09T21:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 4, "2025-08-09T16:00:00Z", "2025-08-09T20:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 6, "2025-08-09T18:00:00Z", "2025-08-10T00:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 11, "2025-08-09T11:00:00Z", "2025-08-09T22:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 20, "2025-08-09T00:00:00Z", "2025-08-09T20:00:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 23, "2025-08-09T00:00:00Z", "2025-08-09T23:00:00Z")]
    public void CalculateHoursPeriodBucket(string timestamp, int hours, string expectedFrom, string expectedTo)
    {
        var period = new SimpleHoursPeriod(hours);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }

}