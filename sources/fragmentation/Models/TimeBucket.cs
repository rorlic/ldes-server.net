namespace AquilaSolutions.LdesServer.Fragmentation.Models;

public class TimeBucket(string from, string to, string key) : IEquatable<TimeBucket>
{
    public const string Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK";

    public string From { get; } = from;
    public string To { get; } = to;

    public string Key { get; } = key;

    public bool Equals(TimeBucket? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return From == other.From && To == other.To && Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TimeBucket)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(From, To, Key);
    }
}