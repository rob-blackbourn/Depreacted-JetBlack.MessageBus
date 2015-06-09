using System.Linq;

namespace JetBlack.MessageBus.Common.Diagnostics
{
    public static class FormatExtensions
    {
        public const string NullValue = "#Null";

        public static string ToFormattedString(this string value)
        {
            return value == null ? NullValue : string.Concat('"', value, '"');
        }

        public static string ToFormattedString(this byte[] value, int maxItems = 20)
        {
            return
                value == null
                    ? NullValue
                    : string.Format(
                        "Length={0}, Bytes=[{1}]{2}",
                        value.Length,
                        string.Join(",", value.Take(maxItems)),
                        value.Length <= maxItems ? string.Empty : " ...");
        }
    }
}
