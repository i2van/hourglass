namespace Hourglass.Extensions;

public static class HotkeyCharExtensions
{
    private const char HotkeyChar = '_';
    private const string HotkeyString = "_";

    extension(string text)
    {
        public string MakeFirstCharHotkey() =>
            string.IsNullOrWhiteSpace(text) || text[0] == HotkeyChar
                ? text
                : $"{HotkeyChar}{text}";

        public string RemoveHotkeyChar() =>
            text.Replace(HotkeyString, string.Empty);
    }
}