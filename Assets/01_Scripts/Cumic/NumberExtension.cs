using System.Globalization;

namespace Cumic
{
    public static class NumberExtension
    {
        public static string ToCommaString(this int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string ToCommaString(this long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        public static string ToCommaString(this float value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
