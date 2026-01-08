using System.Globalization;
using System.Numerics;
using System.Text;

namespace IvoEngine.Expressions.Conversion;

/// <summary>
/// Provides methods for converting various types to strings.
/// These conversions are often used for formatting or serializing data.
/// </summary>
public static class StringConverter
{
    #region Basic to string conversion

    public static string ToString(bool value) => value.ToString();
    public static string ToString(char value) => value.ToString();
    public static string ToString(DateTime value) => value.ToString();
    public static string ToString(DateTimeOffset value) => value.ToString();
    public static string ToString(TimeSpan value) => value.ToString();
    public static string ToString(DateOnly value) => value.ToString();
    public static string ToString(TimeOnly value) => value.ToString();

    public static string ToString(Guid value) => value.ToString();
    public static string ToString(Type value) => value?.FullName ?? string.Empty;
    public static string ToString(Version value) => value?.ToString() ?? string.Empty;
    public static string ToString(Uri value) => value?.ToString() ?? string.Empty;
    public static string ToString(Complex value) => value.ToString();
    public static string ToString(Rune value) => value.ToString();

    public static string ToString<T>(IEnumerable<T> items, string delimiter = ", ")
    {
        if (items == null)
            return "[ ]";

        return $"[ {string.Join(delimiter, items)} ]";
    }

    #endregion

    #region Numeric types to string
    public static string ToString(byte value) => value.ToString();
    public static string ToString(sbyte value) => value.ToString();
    public static string ToString(short value) => value.ToString();
    public static string ToString(ushort value) => value.ToString();
    public static string ToString(int value) => value.ToString();
    public static string ToString(uint value) => value.ToString();
    public static string ToString(long value) => value.ToString();
    public static string ToString(ulong value) => value.ToString();
    public static string ToString(nint value) => value.ToString();
    public static string ToString(nuint value) => value.ToString();
    public static string ToString(float value) => value.ToString();
    public static string ToString(double value) => value.ToString();
    public static string ToString(decimal value) => value.ToString();
    public static string ToString(Half value) => value.ToString();
    public static string ToString(BigInteger value) => value.ToString();
    #endregion

    #region Formatted numeric types to string
    public static string ToFormattedString(byte value, string format) => value.ToString(format);
    public static string ToFormattedString(sbyte value, string format) => value.ToString(format);
    public static string ToFormattedString(short value, string format) => value.ToString(format);
    public static string ToFormattedString(ushort value, string format) => value.ToString(format);
    public static string ToFormattedString(int value, string format) => value.ToString(format);
    public static string ToFormattedString(uint value, string format) => value.ToString(format);
    public static string ToFormattedString(long value, string format) => value.ToString(format);
    public static string ToFormattedString(ulong value, string format) => value.ToString(format);
    public static string ToFormattedString(nint value, string format) => value.ToString(format);
    public static string ToFormattedString(nuint value, string format) => value.ToString(format);
    public static string ToFormattedString(float value, string format) => value.ToString(format);
    public static string ToFormattedString(double value, string format) => value.ToString(format);
    public static string ToFormattedString(decimal value, string format) => value.ToString(format);
    public static string ToFormattedString(Half value, string format) => value.ToString(format);
    public static string ToFormattedString(BigInteger value, string format) => value.ToString(format);

    public static string ToFormattedString(byte value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(sbyte value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(short value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(ushort value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(int value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(uint value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(long value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(ulong value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(nint value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(nuint value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(float value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(double value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(decimal value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(Half value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(BigInteger value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    #endregion

    #region Boolean to string
    public static string ToYesNo(bool value) => value ? "Yes" : "No";
    public static string ToTrueFalse(bool value) => value ? "True" : "False";
    public static string ToOneZero(bool value) => value ? "1" : "0";
    public static string ToEnabledDisabled(bool value) => value ? "Enabled" : "Disabled";
    public static string ToOnOff(bool value) => value ? "On" : "Off";
    #endregion

    #region DateTime and related types to string

    public static string ToFormattedString(DateTime value, string format) => value.ToString(format);
    public static string ToFormattedString(DateTimeOffset value, string format) => value.ToString(format);
    public static string ToFormattedString(TimeSpan value, string format) => value.ToString(format);
    public static string ToFormattedString(DateOnly value, string format) => value.ToString(format);
    public static string ToFormattedString(TimeOnly value, string format) => value.ToString(format);

    public static string ToFormattedString(DateTime value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(DateTimeOffset value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(TimeSpan value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(DateOnly value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);
    public static string ToFormattedString(TimeOnly value, string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);

    public static string ToIso8601(DateTime value) => value.ToString("o");
    public static string ToIso8601(DateTimeOffset value) => value.ToString("o");
    public static string ToShortDateString(DateTime value) => value.ToShortDateString();
    public static string ToLongDateString(DateTime value) => value.ToLongDateString();
    public static string ToShortTimeString(DateTime value) => value.ToShortTimeString();
    public static string ToLongTimeString(DateTime value) => value.ToLongTimeString();
    public static string ToShortDateString(DateOnly value) => value.ToString("d");
    public static string ToLongDateString(DateOnly value) => value.ToString("D");
    public static string ToShortTimeString(TimeOnly value) => value.ToString("t");
    public static string ToLongTimeString(TimeOnly value) => value.ToString("T");
    #endregion

    #region Guid to string
    public static string ToFormattedString(Guid value, string format) => value.ToString(format);
    public static string ToNoDashesString(Guid value) => value.ToString("N");
    public static string ToHyphenatedString(Guid value) => value.ToString("D");
    public static string ToBracedString(Guid value) => value.ToString("B");
    public static string ToParenthesizedString(Guid value) => value.ToString("P");
    #endregion

    #region Other types to string
    public static string ToQualifiedTypeName(Type type)
    {
        if (type == null)
            return string.Empty;

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var typeDefinition = type.GetGenericTypeDefinition().FullName;
            var indexOfBacktick = typeDefinition.IndexOf('`');
            if (indexOfBacktick > 0)
                typeDefinition = typeDefinition.Substring(0, indexOfBacktick);

            return $"{typeDefinition}<{string.Join(", ", Array.ConvertAll(genericArgs, t => ToQualifiedTypeName(t)))}>";
        }

        return type.FullName ?? type.Name;
    }

    public static string ToFriendlyTypeName(Type type)
    {
        if (type == null)
            return string.Empty;

        // Handle simple types with friendly names
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(nint)) return "nint";
        if (type == typeof(nuint)) return "nuint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";
        if (type == typeof(void)) return "void";

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            int rank = type.GetArrayRank();
            string arrayBrackets = rank == 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
            return ToFriendlyTypeName(elementType) + arrayBrackets;
        }

        // Handle generics
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var typeName = type.Name;
            var indexOfBacktick = typeName.IndexOf('`');
            if (indexOfBacktick > 0)
                typeName = typeName.Substring(0, indexOfBacktick);

            return $"{typeName}<{string.Join(", ", Array.ConvertAll(genericArgs, t => ToFriendlyTypeName(t)))}>";
        }

        // Handle other types
        return type.Name;
    }

    public static string ToHexString(byte value) => value.ToString("X2");
    public static string ToHexString(sbyte value) => ((byte)value).ToString("X2");
    public static string ToHexString(short value) => value.ToString("X4");
    public static string ToHexString(ushort value) => value.ToString("X4");
    public static string ToHexString(int value) => value.ToString("X8");
    public static string ToHexString(uint value) => value.ToString("X8");
    public static string ToHexString(long value) => value.ToString("X16");
    public static string ToHexString(ulong value) => value.ToString("X16");

    public static string ToHexString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }

    public static string ToBase64String(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        return Convert.ToBase64String(bytes);
    }
    #endregion

    #region Enum to string
    public static string ToString(Enum value) => value?.ToString() ?? string.Empty;

    public static string ToNumericString(Enum value)
    {
        if (value == null)
            return string.Empty;

        return Convert.ToInt32(value).ToString();
    }

    public static string ToDescription(Enum value)
    {
        if (value == null)
            return string.Empty;

        // Check if enum value has a Description attribute
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attributes = (System.ComponentModel.DescriptionAttribute[])fieldInfo?.GetCustomAttributes(
            typeof(System.ComponentModel.DescriptionAttribute), false);

        return attributes != null && attributes.Length > 0
            ? attributes[0].Description
            : value.ToString();
    }
    #endregion

    #region Nullable types to string
    public static string ToString<T>(T? value) where T : struct => value?.ToString() ?? string.Empty;
    public static string ToString<T>(T? value, string nullText) where T : struct => value?.ToString() ?? nullText;
    public static string ToFormattedString<T>(T? value, string format) where T : struct, IFormattable => value?.ToString(format, null) ?? string.Empty;
    public static string ToFormattedString<T>(T? value, string format, string nullText) where T : struct, IFormattable => value?.ToString(format, null) ?? nullText;
    #endregion

    #region Collections to string
    public static string ToDelimitedString<T>(IEnumerable<T> items, string delimiter = ", ")
    {
        if (items == null)
            return string.Empty;

        return string.Join(delimiter, items);
    }

    public static string ToCsvString<T>(IEnumerable<T> items)
    {
        return ToDelimitedString(items, ",");
    }

    public static string ToJsonArray<T>(IEnumerable<T> items)
    {
        if (items == null)
            return "[]";

        var elements = new List<string>();
        foreach (var item in items)
        {
            if (item == null)
                elements.Add("null");
            else if (item is string str)
                elements.Add($"\"{EscapeJsonString(str)}\"");
            else if (item is bool b)
                elements.Add(b ? "true" : "false");
            else if (item is char c)
                elements.Add($"\"{c}\"");
            else if (item is DateTime dt)
                elements.Add($"\"{dt:o}\"");
            else if (item is DateTimeOffset dto)
                elements.Add($"\"{dto:o}\"");
            else if (item is IFormattable formattable)
                elements.Add(formattable.ToString(null, CultureInfo.InvariantCulture));
            else
                elements.Add(item.ToString());
        }

        return $"[{string.Join(", ", elements)}]";
    }

    private static string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        StringBuilder sb = new StringBuilder(s.Length + 4);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        return sb.ToString();
    }
    #endregion
}