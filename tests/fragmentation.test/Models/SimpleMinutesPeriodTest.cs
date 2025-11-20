using System.Globalization;
using LdesServer.Fragmentation.Models;

namespace LdesServer.Fragmentation.Test.Models;

public class SimpleMinutesPeriodTest
{
    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-08-09T18:05:00Z", "2025-08-09T18:06:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 2, "2025-08-09T18:04:00Z", "2025-08-09T18:06:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 3, "2025-08-09T18:03:00Z", "2025-08-09T18:06:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 4, "2025-08-09T18:04:00Z", "2025-08-09T18:08:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 6, "2025-08-09T18:00:00Z", "2025-08-09T18:06:00Z")]
    [InlineData("2025-08-09T18:05:09Z", 12, "2025-08-09T18:00:00Z", "2025-08-09T18:12:00Z")]
    [InlineData("2025-08-09T18:15:09Z", 15, "2025-08-09T18:15:00Z", "2025-08-09T18:30:00Z")]
    [InlineData("2025-08-09T18:45:09Z", 20, "2025-08-09T18:40:00Z", "2025-08-09T19:00:00Z")]
    [InlineData("2025-08-09T18:45:09Z", 30, "2025-08-09T18:30:00Z", "2025-08-09T19:00:00Z")]
    public void CalculateMinutesPeriodBucket(string timestamp, int minutes, string expectedFrom, string expectedTo)
    {
        var period = new SimpleMinutesPeriod(minutes);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }

}