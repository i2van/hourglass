namespace Hourglass.Extensions;

public static class HotkeyCharExtensions
{
    private const char HotkeyChar = '_';
    private const string HotkeyString = "_";

    public static string MakeFirstCharHotkey(this string text) =>
        string.IsNullOrWhiteSpace(text) || text[0] == HotkeyChar
            ? text
            : $"{HotkeyChar}{text}";

    public static string RemoveHotkeyChar(this string text) =>
        text.Replace(HotkeyString, string.Empty);
}