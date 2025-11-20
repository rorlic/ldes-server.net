using System.Globalization;
using LdesServer.Fragmentation.Models;

namespace LdesServer.Fragmentation.Test.Models;

public class SimpleSecondsPeriodTest
{
    
    [Theory]
    [InlineData("2025-08-09T18:05:09Z", 1, "2025-08-09T18:05:09Z", "2025-08-09T18:05:10Z")]
    [InlineData("2025-08-09T18:05:09Z", 2, "2025-08-09T18:05:08Z", "2025-08-09T18:05:10Z")]
    [InlineData("2025-08-09T18:05:09Z", 3, "2025-08-09T18:05:09Z", "2025-08-09T18:05:12Z")]
    [InlineData("2025-08-09T18:05:09Z", 4, "2025-08-09T18:05:08Z", "2025-08-09T18:05:12Z")]
    [InlineData("2025-08-09T18:05:09Z", 6, "2025-08-09T18:05:06Z", "2025-08-09T18:05:12Z")]
    [InlineData("2025-08-09T18:05:09Z", 12, "2025-08-09T18:05:00Z", "2025-08-09T18:05:12Z")]
    [InlineData("2025-08-09T18:05:09Z", 15, "2025-08-09T18:05:00Z", "2025-08-09T18:05:15Z")]
    [InlineData("2025-08-09T18:05:19Z", 15, "2025-08-09T18:05:15Z", "2025-08-09T18:05:30Z")]
    [InlineData("2025-08-09T18:05:59Z", 30, "2025-08-09T18:05:30Z", "2025-08-09T18:06:00Z")]
    public void CalculateSecondsPeriodBucket(string timestamp, int seconds, string expectedFrom, string expectedTo)
    {
        var period = new SimpleSecondsPeriod(seconds);
        var dateTime = DateTimeOffset.Parse(timestamp, null, DateTimeStyles.RoundtripKind);
        var actual = period.CalculateBucket(dateTime);
        Assert.Equal(expectedFrom, actual.From);
        Assert.Equal(expectedTo, actual.To);
    }
}