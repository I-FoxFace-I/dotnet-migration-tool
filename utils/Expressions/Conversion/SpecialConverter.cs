using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils.Expressions.Conversion;

/// <summary>
/// Provides special conversions that are not natively available in C#.
/// Focuses mainly on conversions between strings and other types
/// and conversions between enums and other types.
/// </summary>
public static class SpecialConverter
{
    #region String conversions
    public static byte ToByte(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (byte.TryParse(value, out byte result))
            return result;

        return 0;
    }

    public static sbyte ToSByte(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (sbyte.TryParse(value, out sbyte result))
            return result;

        return 0;
    }

    public static short ToInt16(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (short.TryParse(value, out short result))
            return result;

        return 0;
    }

    public static ushort ToUInt16(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (ushort.TryParse(value, out ushort result))
            return result;

        return 0;
    }

    public static int ToInt32(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (int.TryParse(value, out int result))
            return result;

        return 0;
    }

    public static uint ToUInt32(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (uint.TryParse(value, out uint result))
            return result;

        return 0;
    }

    public static long ToInt64(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (long.TryParse(value, out long result))
            return result;

        return 0;
    }

    public static ulong ToUInt64(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (ulong.TryParse(value, out ulong result))
            return result;

        return 0;
    }

    public static nint ToNInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (nint.TryParse(value, out nint result))
            return result;

        return 0;
    }

    public static nuint ToNUInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (nuint.TryParse(value, out nuint result))
            return result;

        return 0;
    }

    public static float ToSingle(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (float.TryParse(value, out float result))
            return result;

        return 0;
    }

    public static double ToDouble(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (double.TryParse(value, out double result))
            return result;

        return 0;
    }

    public static decimal ToDecimal(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (decimal.TryParse(value, out decimal result))
            return result;

        return 0;
    }

    public static bool ToBoolean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Check for various boolean string representations
        if (bool.TryParse(value, out bool result))
            return result;

        // Check for numeric values (0 = false, non-zero = true)
        if (int.TryParse(value, out int intValue))
            return intValue != 0;

        // Check for "yes"/"no" values
        value = value.Trim().ToLowerInvariant();
        if (value == "yes" || value == "y" || value == "true" || value == "t" || value == "1")
            return true;

        if (value == "no" || value == "n" || value == "false" || value == "f" || value == "0")
            return false;

        return false;
    }

    public static char ToChar(string value)
    {
        if (string.IsNullOrEmpty(value))
            return '\0';

        if (value.Length == 1)
            return value[0];

        if (char.TryParse(value, out char result))
            return result;

        return '\0';
    }

    public static DateTime ToDateTime(string value)
    {
        if (string.IsNullOrEmpty(value))
            return DateTime.MinValue;

        if (DateTime.TryParse(value, out DateTime result))
            return result;

        return DateTime.MinValue;
    }

    public static DateTimeOffset ToDateTimeOffset(string value)
    {
        if (string.IsNullOrEmpty(value))
            return DateTimeOffset.MinValue;

        if (DateTimeOffset.TryParse(value, out DateTimeOffset result))
            return result;

        return DateTimeOffset.MinValue;
    }

    public static DateOnly ToDateOnly(string value)
    {
        if (string.IsNullOrEmpty(value))
            return DateOnly.MinValue;

        if (DateOnly.TryParse(value, out DateOnly result))
            return result;

        return DateOnly.MinValue;
    }

    public static TimeOnly ToTimeOnly(string value)
    {
        if (string.IsNullOrEmpty(value))
            return TimeOnly.MinValue;

        if (TimeOnly.TryParse(value, out TimeOnly result))
            return result;

        return TimeOnly.MinValue;
    }

    public static Guid ToGuid(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Guid.Empty;

        if (Guid.TryParse(value, out Guid result))
            return result;

        return Guid.Empty;
    }

    public static TimeSpan ToTimeSpan(string value)
    {
        if (string.IsNullOrEmpty(value))
            return TimeSpan.Zero;

        if (TimeSpan.TryParse(value, out TimeSpan result))
            return result;

        return TimeSpan.Zero;
    }

    public static Version ToVersion(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new Version(0, 0);

        if (Version.TryParse(value, out Version result))
            return result;

        return new Version(0, 0);
    }

    public static Uri ToUri(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri result))
            return result;

        return null;
    }

    public static BigInteger ToBigInteger(string value)
    {
        if (string.IsNullOrEmpty(value))
            return BigInteger.Zero;

        if (BigInteger.TryParse(value, out BigInteger result))
            return result;

        return BigInteger.Zero;
    }

    public static Half ToHalf(string value)
    {
        if (string.IsNullOrEmpty(value))
            return (Half)0.0f;

        if (Half.TryParse(value, out Half result))
            return result;

        return (Half)0.0f;
    }

    public static Complex ToComplex(string value)
    {
        if (string.IsNullOrEmpty(value))
            return Complex.Zero;

        // Try to parse a string like "a+bi" or "a-bi"
        value = value.Replace(" ", "");

        // Check for pure imaginary form (bi)
        if (value.EndsWith("i", StringComparison.OrdinalIgnoreCase))
        {
            string imaginary = value.Substring(0, value.Length - 1);
            if (double.TryParse(imaginary, out double imaginaryPart))
                return new Complex(0, imaginaryPart);
        }

        // Match patterns like "a+bi" or "a-bi"
        var regex = new Regex(@"^([-+]?\d*\.?\d*)([-+]\d*\.?\d*)[iI]$");
        var match = regex.Match(value);

        if (match.Success && match.Groups.Count >= 3)
        {
            if (double.TryParse(match.Groups[1].Value, out double real) &&
                double.TryParse(match.Groups[2].Value, out double imaginary))
            {
                return new Complex(real, imaginary);
            }
        }

        // Try to parse just a real number
        if (double.TryParse(value, out double realPart))
            return new Complex(realPart, 0);

        return Complex.Zero;
    }

    public static Rune ToRune(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new Rune('\0');

        if (value.Length == 1)
            return new Rune(value[0]);

        if (value.Length == 2 && char.IsHighSurrogate(value[0]) && char.IsLowSurrogate(value[1]))
            return new Rune(char.ConvertToUtf32(value[0], value[1]));

        if (int.TryParse(value, out int codePoint) && Rune.IsValid(codePoint))
            return new Rune(codePoint);

        return new Rune('\0');
    }

    public static Type ToType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return Type.GetType(value, false, true);
        }
        catch
        {
            return null;
        }
    }

    public static Range ToRange(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new Range(Index.Start, Index.End);

        // Try to parse a string like "start..end"
        var parts = value.Split("..", StringSplitOptions.None);
        if (parts.Length == 2)
        {
            Index start, end;

            if (parts[0] == "")
                start = Index.Start;
            else if (int.TryParse(parts[0], out int startValue))
                start = new Index(startValue, false);
            else
                return new Range(Index.Start, Index.End);

            if (parts[1] == "")
                end = Index.End;
            else if (int.TryParse(parts[1], out int endValue))
                end = new Index(endValue, parts[1].EndsWith("^"));
            else
                return new Range(Index.Start, Index.End);

            return new Range(start, end);
        }

        return new Range(Index.Start, Index.End);
    }
    #endregion

    #region Enum conversions
    public static TEnum ToEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
            return default;

        if (Enum.TryParse(value, true, out TEnum result))
            return result;

        // Try parsing as number
        if (int.TryParse(value, out int intValue) && Enum.IsDefined(typeof(TEnum), intValue))
            return (TEnum)Enum.ToObject(typeof(TEnum), intValue);

        return default;
    }

    public static TEnum ToEnum<TEnum>(int value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(byte value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(sbyte value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(short value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(ushort value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(uint value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(long value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static TEnum ToEnum<TEnum>(ulong value) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
            return (TEnum)Enum.ToObject(typeof(TEnum), value);

        return default;
    }

    public static int ToInt32(Enum value)
    {
        return Convert.ToInt32(value);
    }

    public static string ToString(Enum value)
    {
        return value?.ToString();
    }
    #endregion

    #region Collection conversions
    public static List<T> ToList<T>(T[] array)
    {
        if (array == null)
            return new List<T>();

        return new List<T>(array);
    }

    public static T[] ToArray<T>(List<T> list)
    {
        if (list == null)
            return Array.Empty<T>();

        return list.ToArray();
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        where TKey : notnull
    {
        if (pairs == null)
            return new Dictionary<TKey, TValue>();

        return new Dictionary<TKey, TValue>(pairs);
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(IEnumerable<T> items)
    {
        if (items == null)
            return Array.Empty<T>();

        return items is IReadOnlyList<T> roList ? roList : new List<T>(items).AsReadOnly();
    }

    public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if (dictionary == null)
            return new Dictionary<TKey, TValue>();

        return new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

    public static HashSet<T> ToHashSet<T>(IEnumerable<T> items)
    {
        if (items == null)
            return new HashSet<T>();

        return new HashSet<T>(items);
    }
    #endregion

    #region Nullable conversions
    public static T? ToNullable<T>(T value) where T : struct
    {
        return value;
    }

    public static T? ToNullable<T>(object value) where T : struct
    {
        if (value == null)
            return null;

        if (value is T tValue)
            return tValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return null;
        }
    }

    public static T ToNonNullable<T>(T? value) where T : struct
    {
        return value ?? default;
    }

    public static bool HasValue<T>(T? value) where T : struct
    {
        return value.HasValue;
    }
    #endregion

    #region KeyValuePair conversions
    public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(TKey key, TValue value)
    {
        return new KeyValuePair<TKey, TValue>(key, value);
    }

    public static Tuple<TKey, TValue> ToTuple<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
    {
        return new Tuple<TKey, TValue>(pair.Key, pair.Value);
    }

    public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(Tuple<TKey, TValue> tuple)
    {
        return new KeyValuePair<TKey, TValue>(tuple.Item1, tuple.Item2);
    }
    #endregion

    #region String to IntPtr/UIntPtr conversions
    public static nint ToIntPtr(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (nint.TryParse(value, out nint result))
            return result;

        return 0;
    }

    public static nuint ToUIntPtr(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        if (nuint.TryParse(value, out nuint result))
            return result;

        return 0;
    }
    #endregion
}