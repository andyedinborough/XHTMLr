using System;
using System.Collections;

namespace XHTMLr {
    internal static class Extensions {
        public static string NotEmpty(this string input, string @default) {
            return input.IsNullOrEmpty() ? @default : input;
        }
        public static string NotNull(this string input) {
            return input ?? string.Empty;
        }

        public static bool IsNullOrEmpty(this IEnumerable items) {
            if (items == null) return true;

            if (items is Array) return ((Array)items).Length == 0;
            if (items is ICollection) return ((ICollection)items).Count == 0;

            return !items.GetEnumerator().MoveNext();
        }

        public static int ToInt(this string input, int DefaultValue = 0) {
            int value;
            if (int.TryParse(input, out  value)) {
                return value;
            } else {
                return DefaultValue;
            }
        }

        public static bool Between<T>(this T a, T b, T c) where T : IComparable<T> {
            int ab = a.CompareTo(b);
            if (ab == 0) return true;
            int ac = a.CompareTo(c);
            if (ac == 0) return true;
            if (ab + ac == 0) return true;
            return false;
        }

        public static T AtMost<T>(this T input, T max) where T : struct,IComparable<T> {
            if (input.CompareTo(max) == 1) return max;
            return input;
        }

        public static int HexToInt(this string input, int defaultValue = 0) {
            int ret;
            if (int.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out ret))
                return ret;
            return defaultValue;
        }

        public static string Left(this string input, int length) {
            input = input ?? string.Empty;
            return input.Substring(0, length.AtMost(input.Length));
        }

        public static bool Contains<T>(this T bitmask, T flag) where T : struct, IConvertible {
            int v1 = Convert.ToInt32(bitmask);
            int v2 = Convert.ToInt32(flag);
            return (v1 & v2) == v2;
        }
    }
}
