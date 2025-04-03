using System.IO.Compression;
using System.Text;

namespace KCD2.PAK;

public static class PakHelper
{
    private const int ValidZipDate_YearMin = 1980;

    private const int ValipZipDate_YearMax = 2107;

    private static readonly DateTime _invalidDateIndicator = new(ValidZipDate_YearMin, 1, 1, 0, 0, 0);

    public static uint DateTimeToDosTime(DateTime dateTime)
    {
        if (dateTime.Year < ValidZipDate_YearMin || dateTime.Year > ValipZipDate_YearMax)
            return DateTimeToDosTime(_invalidDateIndicator);

        int ret = (dateTime.Year - ValidZipDate_YearMin) & 0x7F;
        ret = (ret << 4) + dateTime.Month;
        ret = (ret << 5) + dateTime.Day;
        ret = (ret << 5) + dateTime.Hour;
        ret = (ret << 6) + dateTime.Minute;
        ret = (ret << 5) + (dateTime.Second / 2);

        return (uint)ret;
    }

    public static byte[] GetEncodedTruncatedBytesFromString(string? text, Encoding? encoding, int maxBytes, out bool isUTF8)
    {
        if (string.IsNullOrEmpty(text))
        {
            isUTF8 = false;
            return [];
        }

        encoding ??= GetEncoding(text);
        isUTF8 = encoding.CodePage == 65001;

        if (maxBytes == 0)
            return encoding.GetBytes(text);

        byte[] bytes;
        if (isUTF8 && encoding.GetMaxByteCount(text.Length) > maxBytes)
        {
            int totalCodePoints = 0;
            foreach (Rune rune in text.EnumerateRunes())
            {
                if (totalCodePoints + rune.Utf8SequenceLength > maxBytes)
                    break;
                totalCodePoints += rune.Utf8SequenceLength;
            }

            bytes = encoding.GetBytes(text);
            return bytes[0..totalCodePoints];
        }

        bytes = encoding.GetBytes(text);
        return maxBytes < bytes.Length ? bytes[0..maxBytes] : bytes;
    }

    public static Encoding GetEncoding(string text) => text.AsSpan().ContainsAnyExceptInRange((char)32, (char)126) ? Encoding.UTF8 : Encoding.ASCII;
}