namespace Hourglass.Parsing;

/// <summary>
/// A time span offset that is added to a time.
/// </summary>
public record Offset(TimeSpanToken Value);

/// <summary>
/// A time span offset that is subtracted from a time.
/// </summary>
public sealed record NegativeOffset(TimeSpanToken Value) : Offset(Value);
