using System.Numerics;
using System.Text;

namespace IvoEngine.Expressions.Conversion;

/// <summary>
/// Provides explicit conversions between primitive types.
/// Explicit conversions are those where data or precision may potentially be lost.
/// The class tries to handle edge cases, such as overflow or loss of precision.
/// </summary>
public static class ExplicitConverter
{

    #region ToByte conversions (to byte)
    public static byte ToByte(sbyte value) => value < 0 ? (byte)0 : (byte)value;
    public static byte ToByte(short value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(ushort value) => value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(int value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(uint value) => value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(long value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(ulong value) => value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(nint value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(nuint value) => value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(float value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)Math.Round(value);
    public static byte ToByte(double value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)Math.Round(value);
    public static byte ToByte(decimal value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)Math.Round(value);
    public static byte ToByte(char value) => value > byte.MaxValue ? byte.MaxValue : (byte)value;
    public static byte ToByte(bool value) => value ? (byte)1 : (byte)0;
    public static byte ToByte(Half value) => (float)value < 0 ? (byte)0 : (float)value > byte.MaxValue ? byte.MaxValue : (byte)Math.Round((float)value);
    public static byte ToByte(BigInteger value) => value < 0 ? (byte)0 : value > byte.MaxValue ? byte.MaxValue : (byte)value;
    #endregion

    #region ToSByte conversions (to sbyte)
    public static sbyte ToSByte(byte value) => value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(short value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(ushort value) => value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(int value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(uint value) => value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(long value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(ulong value) => value > (ulong)sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(nint value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(nuint value) => value > (nuint)sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(float value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)Math.Round(value);
    public static sbyte ToSByte(double value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)Math.Round(value);
    public static sbyte ToSByte(decimal value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)Math.Round(value);
    public static sbyte ToSByte(char value) => value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    public static sbyte ToSByte(bool value) => value ? (sbyte)1 : (sbyte)0;
    public static sbyte ToSByte(Half value) => (float)value < sbyte.MinValue ? sbyte.MinValue : (float)value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)Math.Round((float)value);
    public static sbyte ToSByte(BigInteger value) => value < sbyte.MinValue ? sbyte.MinValue : value > sbyte.MaxValue ? sbyte.MaxValue : (sbyte)value;
    #endregion

    #region ToInt16 conversions (to short)
    public static short ToInt16(ushort value) => value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(int value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(uint value) => value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(long value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(ulong value) => value > (ulong)short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(nint value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(nuint value) => value > (nuint)short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(float value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)Math.Round(value);
    public static short ToInt16(double value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)Math.Round(value);
    public static short ToInt16(decimal value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)Math.Round(value);
    public static short ToInt16(char value) => value > short.MaxValue ? short.MaxValue : (short)value;
    public static short ToInt16(bool value) => value ? (short)1 : (short)0;
    public static short ToInt16(Half value) => (float)value < short.MinValue ? short.MinValue : (float)value > short.MaxValue ? short.MaxValue : (short)Math.Round((float)value);
    public static short ToInt16(BigInteger value) => value < short.MinValue ? short.MinValue : value > short.MaxValue ? short.MaxValue : (short)value;
    #endregion

    #region ToUInt16 conversions (to ushort)
    public static ushort ToUInt16(sbyte value) => value < 0 ? (ushort)0 : (ushort)value;
    public static ushort ToUInt16(short value) => value < 0 ? (ushort)0 : (ushort)value;
    public static ushort ToUInt16(int value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(uint value) => value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(long value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(ulong value) => value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(nint value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(nuint value) => value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    public static ushort ToUInt16(float value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)Math.Round(value);
    public static ushort ToUInt16(double value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)Math.Round(value);
    public static ushort ToUInt16(decimal value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)Math.Round(value);
    public static ushort ToUInt16(bool value) => value ? (ushort)1 : (ushort)0;
    public static ushort ToUInt16(Half value) => (float)value < 0 ? (ushort)0 : (float)value > ushort.MaxValue ? ushort.MaxValue : (ushort)Math.Round((float)value);
    public static ushort ToUInt16(BigInteger value) => value < 0 ? (ushort)0 : value > ushort.MaxValue ? ushort.MaxValue : (ushort)value;
    #endregion

    #region ToInt32 conversions (to int)
    public static int ToInt32(uint value) => value > int.MaxValue ? int.MaxValue : (int)value;
    public static int ToInt32(long value) => value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)value;
    public static int ToInt32(ulong value) => value > int.MaxValue ? int.MaxValue : (int)value;
    public static int ToInt32(nint value) => (int)value;
    public static int ToInt32(nuint value) => value > int.MaxValue ? int.MaxValue : (int)value;
    public static int ToInt32(float value) => value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)Math.Round(value);
    public static int ToInt32(double value) => value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)Math.Round(value);
    public static int ToInt32(decimal value) => value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)Math.Round(value);
    public static int ToInt32(bool value) => value ? 1 : 0;
    public static int ToInt32(Half value) => (float)value < int.MinValue ? int.MinValue : (float)value > int.MaxValue ? int.MaxValue : (int)Math.Round((float)value);
    public static int ToInt32(BigInteger value) => value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)value;
    #endregion

    #region ToUInt32 conversions (to uint)
    public static uint ToUInt32(sbyte value) => value < 0 ? 0U : (uint)value;
    public static uint ToUInt32(short value) => value < 0 ? 0U : (uint)value;
    public static uint ToUInt32(int value) => value < 0 ? 0U : (uint)value;
    public static uint ToUInt32(long value) => value < 0 ? 0U : value > uint.MaxValue ? uint.MaxValue : (uint)value;
    public static uint ToUInt32(ulong value) => value > uint.MaxValue ? uint.MaxValue : (uint)value;
    public static uint ToUInt32(nint value) => value < 0 ? 0U : (uint)value;
    public static uint ToUInt32(nuint value) => (uint)value;
    public static uint ToUInt32(float value) => value < 0 ? 0U : value > uint.MaxValue ? uint.MaxValue : (uint)Math.Round(value);
    public static uint ToUInt32(double value) => value < 0 ? 0U : value > uint.MaxValue ? uint.MaxValue : (uint)Math.Round(value);
    public static uint ToUInt32(decimal value) => value < 0 ? 0U : value > uint.MaxValue ? uint.MaxValue : (uint)Math.Round(value);
    public static uint ToUInt32(bool value) => value ? 1U : 0U;
    public static uint ToUInt32(Half value) => (float)value < 0 ? 0U : (float)value > uint.MaxValue ? uint.MaxValue : (uint)Math.Round((float)value);
    public static uint ToUInt32(BigInteger value) => value < 0 ? 0U : value > uint.MaxValue ? uint.MaxValue : (uint)value;
    #endregion

    #region ToInt64 conversions (to long)
    public static long ToInt64(ulong value) => value > long.MaxValue ? long.MaxValue : (long)value;
    public static long ToInt64(float value) => value < long.MinValue ? long.MinValue : value > long.MaxValue ? long.MaxValue : (long)Math.Round(value);
    public static long ToInt64(double value) => value < long.MinValue ? long.MinValue : value > long.MaxValue ? long.MaxValue : (long)Math.Round(value);
    public static long ToInt64(decimal value) => value < long.MinValue ? long.MinValue : value > long.MaxValue ? long.MaxValue : (long)Math.Round(value);
    public static long ToInt64(bool value) => value ? 1L : 0L;
    public static long ToInt64(Half value) => (float)value < long.MinValue ? long.MinValue : (float)value > long.MaxValue ? long.MaxValue : (long)Math.Round((float)value);
    public static long ToInt64(BigInteger value) => value < long.MinValue ? long.MinValue : value > long.MaxValue ? long.MaxValue : (long)value;
    #endregion

    #region ToUInt64 conversions (to ulong)
    public static ulong ToUInt64(sbyte value) => value < 0 ? 0UL : (ulong)value;
    public static ulong ToUInt64(short value) => value < 0 ? 0UL : (ulong)value;
    public static ulong ToUInt64(int value) => value < 0 ? 0UL : (ulong)value;
    public static ulong ToUInt64(long value) => value < 0 ? 0UL : (ulong)value;
    public static ulong ToUInt64(nint value) => value < 0 ? 0UL : (ulong)value;
    public static ulong ToUInt64(float value) => value < 0 ? 0UL : value > ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Round(value);
    public static ulong ToUInt64(double value) => value < 0 ? 0UL : value > ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Round(value);
    public static ulong ToUInt64(decimal value) => value < 0 ? 0UL : value > ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Round(value);
    public static ulong ToUInt64(bool value) => value ? 1UL : 0UL;
    public static ulong ToUInt64(Half value) => (float)value < 0 ? 0UL : (float)value > ulong.MaxValue ? ulong.MaxValue : (ulong)Math.Round((float)value);
    public static ulong ToUInt64(BigInteger value) => value < 0 ? 0UL : value > ulong.MaxValue ? ulong.MaxValue : (ulong)value;
    #endregion

    #region ToSingle conversions (to float)
    public static float ToSingle(double value) => value < float.MinValue ? float.MinValue : value > float.MaxValue ? float.MaxValue : (float)value;
    public static float ToSingle(decimal value) => (float)value;
    public static float ToSingle(bool value) => value ? 1.0f : 0.0f;
    public static float ToSingle(BigInteger value)
    {
        if (value < (BigInteger)float.MinValue) return float.MinValue;
        if (value > (BigInteger)float.MaxValue) return float.MaxValue;
        return (float)value;
    }
    #endregion

    #region ToDouble conversions (to double)
    public static double ToDouble(decimal value) => (double)value;
    public static double ToDouble(bool value) => value ? 1.0 : 0.0;
    public static double ToDouble(BigInteger value)
    {
        if (value < (BigInteger)double.MinValue) return double.MinValue;
        if (value > (BigInteger)double.MaxValue) return double.MaxValue;
        return (double)value;
    }
    #endregion

    #region ToDecimal conversions (to decimal)
    public static decimal ToDecimal(float value) => value < (float)decimal.MinValue ? decimal.MinValue : value > (float)decimal.MaxValue ? decimal.MaxValue : (decimal)value;
    public static decimal ToDecimal(double value) => value < (double)decimal.MinValue ? decimal.MinValue : value > (double)decimal.MaxValue ? decimal.MaxValue : (decimal)value;
    public static decimal ToDecimal(bool value) => value ? 1.0m : 0.0m;
    public static decimal ToDecimal(BigInteger value)
    {
        if (value < (BigInteger)decimal.MinValue) return decimal.MinValue;
        if (value > (BigInteger)decimal.MaxValue) return decimal.MaxValue;
        return (decimal)value;
    }
    #endregion

    #region ToHalf conversions (to Half)
    public static Half ToHalf(float value) => value < (float)Half.MinValue ? Half.MinValue : value > (float)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(double value) => value < (double)Half.MinValue ? Half.MinValue : value > (double)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(decimal value) => value < (decimal)(float)Half.MinValue ? Half.MinValue : value > (decimal)(float)Half.MaxValue ? Half.MaxValue : (Half)(float)value;
    public static Half ToHalf(byte value) => (Half)value;
    public static Half ToHalf(sbyte value) => (Half)value;
    public static Half ToHalf(short value) => (Half)value;
    public static Half ToHalf(ushort value) => (Half)value;
    public static Half ToHalf(int value) => value < (int)(float)Half.MinValue ? Half.MinValue : value > (int)(float)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(uint value) => value > (uint)(float)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(long value) => value < (long)(float)Half.MinValue ? Half.MinValue : value > (long)(float)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(ulong value) => value > (ulong)(float)Half.MaxValue ? Half.MaxValue : (Half)value;
    public static Half ToHalf(bool value) => value ? (Half)1.0f : (Half)0.0f;
    #endregion

    #region ToBigInteger conversions (to BigInteger)
    public static BigInteger ToBigInteger(float value) => (BigInteger)value;
    public static BigInteger ToBigInteger(double value) => (BigInteger)value;
    public static BigInteger ToBigInteger(decimal value) => (BigInteger)value;
    public static BigInteger ToBigInteger(bool value) => value ? BigInteger.One : BigInteger.Zero;
    #endregion

    //#region ToNInt conversions (to nint)
    //public static nint ToNInt(uint value) => value > (uint)nint.MaxValue ? nint.MaxValue : (nint)value;
    //public static nint ToNInt(long value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    //public static nint ToNInt(ulong value) => value > (ulong)nint.MaxValue ? nint.MaxValue : (nint)value;
    //public static nint ToNInt(nuint value) => value > (nuint)nint.MaxValue ? nint.MaxValue : (nint)value;
    //public static nint ToNInt(float value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)Math.Round(value);
    //public static nint ToNInt(double value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)Math.Round(value);
    //public static nint ToNInt(decimal value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)Math.Round(value);
    //public static nint ToNInt(bool value) => value ? (nint)1 : (nint)0;
    //public static nint ToNInt(Half value) => (float)value < nint.MinValue ? nint.MinValue : (float)value > nint.MaxValue ? nint.MaxValue : (nint)Math.Round((float)value);
    //public static nint ToNInt(BigInteger value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    //#endregion

    //#region ToNUInt conversions (to nuint)
    //public static nuint ToNUInt(sbyte value) => value < 0 ? (nuint)0 : (nuint)value;
    //public static nuint ToNUInt(short value) => value < 0 ? (nuint)0 : (nuint)value;
    //public static nuint ToNUInt(int value) => value < 0 ? (nuint)0 : (nuint)value;
    //public static nuint ToNUInt(long value) => value < 0 ? (nuint)0 : value > (long)nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    //public static nuint ToNUInt(ulong value) => value > nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    //public static nuint ToNUInt(nint value) => value < 0 ? (nuint)0 : (nuint)value;
    //public static nuint ToNUInt(float value) => value < 0 ? (nuint)0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)Math.Round(value);
    //public static nuint ToNUInt(double value) => value < 0 ? (nuint)0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)Math.Round(value);
    //public static nuint ToNUInt(decimal value) => value < 0 ? (nuint)0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)Math.Round(value);
    //public static nuint ToNUInt(bool value) => value ? (nuint)1 : (nuint)0;
    //public static nuint ToNUInt(Half value) => (float)value < 0 ? (nuint)0 : (float)value > nuint.MaxValue ? nuint.MaxValue : (nuint)Math.Round((float)value);
    //public static nuint ToNUInt(BigInteger value) => value < 0 ? (nuint)0 : value > (BigInteger)nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    //#endregion

    #region ToBoolean conversions (to bool)
    public static bool ToBoolean(byte value) => value != 0;
    public static bool ToBoolean(sbyte value) => value != 0;
    public static bool ToBoolean(short value) => value != 0;
    public static bool ToBoolean(ushort value) => value != 0;
    public static bool ToBoolean(int value) => value != 0;
    public static bool ToBoolean(uint value) => value != 0;
    public static bool ToBoolean(long value) => value != 0;
    public static bool ToBoolean(ulong value) => value != 0;
    public static bool ToBoolean(nint value) => value != 0;
    public static bool ToBoolean(nuint value) => value != 0;
    public static bool ToBoolean(float value) => value != 0;
    public static bool ToBoolean(double value) => value != 0;
    public static bool ToBoolean(decimal value) => value != 0;
    public static bool ToBoolean(Half value) => (float)value != 0;
    public static bool ToBoolean(BigInteger value) => value != 0;
    #endregion

    #region ToChar conversions (to char)
    public static char ToChar(byte value) => (char)value;
    public static char ToChar(sbyte value) => value < 0 ? (char)0 : (char)value;
    public static char ToChar(short value) => value < 0 ? (char)0 : value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(ushort value) => (char)value;
    public static char ToChar(int value) => value < 0 ? (char)0 : value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(uint value) => value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(long value) => value < 0 ? (char)0 : value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(ulong value) => value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(nint value) => value < 0 ? (char)0 : value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(nuint value) => value > char.MaxValue ? char.MaxValue : (char)value;
    public static char ToChar(Rune rune) => (char)rune.Value;
    #endregion

    #region ToDateTime conversions (to DateTime)
    public static DateTime ToDateTime(long ticks) => new DateTime(ticks);
    #endregion

    #region ToDateOnly conversions (to DateOnly)
    public static DateOnly ToDateOnly(DateTime dateTime) => DateOnly.FromDateTime(dateTime);
    #endregion

    #region ToTimeOnly conversions (to TimeOnly)
    public static TimeOnly ToTimeOnly(DateTime dateTime) => TimeOnly.FromDateTime(dateTime);
    public static TimeOnly ToTimeOnly(TimeSpan timeSpan) => TimeOnly.FromTimeSpan(timeSpan);
    #endregion

    #region ToRune conversions (to Rune)
    public static Rune ToRune(int value)
    {
        // Check for valid Unicode code point range
        if (value < 0 || value > 0x10FFFF || value >= 0xD800 && value <= 0xDFFF)
            return new Rune('\0'); // Returns null character as fallback

        return new Rune(value);
    }

    public static Rune ToRune(uint value)
    {
        // Check for valid Unicode code point range
        if (value > 0x10FFFF || value >= 0xD800 && value <= 0xDFFF)
            return new Rune('\0'); // Returns null character as fallback

        return new Rune((int)value);
    }

    public static Rune ToRune(char value)
    {
        // Check for surrogate pairs
        if (char.IsSurrogate(value))
            return new Rune('\0'); // Returns null character as fallback

        return new Rune(value);
    }

    #endregion

    #region ToComplex conversions
    public static Complex ToComplex(decimal value) => new Complex((double)value, 0);
    #endregion

    #region ToDateOnly conversions
    public static DateOnly ToDateOnly(DateTimeOffset value) => DateOnly.FromDateTime(value.DateTime);
    #endregion

    #region ToDateTime conversions
    public static DateTime ToDateTime(DateOnly value) => value.ToDateTime(TimeOnly.MinValue);
    public static DateTime ToDateTime(DateTimeOffset value) => value.DateTime;
    #endregion

    #region ToDateTimeOffset conversions
    public static DateTimeOffset ToDateTimeOffset(DateOnly value) => new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue));
    public static DateTimeOffset ToDateTimeOffset(DateTime value) => new DateTimeOffset(value);
    #endregion

    #region ToDecimal conversions
    public static decimal ToDecimal(Complex value)
    {
        if (value.Imaginary != 0)
            return (decimal)value.Real; // Ignore imaginary part
        return (decimal)value.Real;
    }
    public static decimal ToDecimal(Half value) => (decimal)(float)value;
    #endregion

    #region ToDouble conversions
    public static double ToDouble(Complex value)
    {
        if (value.Imaginary != 0)
            return value.Real; // Ignore imaginary part
        return value.Real;
    }
    #endregion

    #region ToChar conversions
    public static char ToChar(BigInteger value) => value < 0 ? '\0' : value > char.MaxValue ? char.MaxValue : (char)(ushort)value;
    public static char ToChar(decimal value) => value < 0 ? '\0' : value > char.MaxValue ? char.MaxValue : (char)(ushort)value;
    public static char ToChar(double value) => value < 0 ? '\0' : value > char.MaxValue ? char.MaxValue : (char)(ushort)value;
    public static char ToChar(Half value) => (float)value < 0 ? '\0' : (float)value > char.MaxValue ? char.MaxValue : (char)(ushort)(float)value;
    public static char ToChar(float value) => value < 0 ? '\0' : value > char.MaxValue ? char.MaxValue : (char)(ushort)value;
    #endregion

    //#region ToIndex conversions
    //public static Index ToIndex(int value) => new Index(Math.Max(0, value), false);
    //#endregion

    #region ToInt32 conversions
    public static int ToInt32(Complex value)
    {
        if (value.Real < int.MinValue) return int.MinValue;
        if (value.Real > int.MaxValue) return int.MaxValue;
        return (int)value.Real; // Ignore imaginary part
    }
    public static int ToInt32(Index value) => value.Value;
    public static int ToInt32(Rune value) => value.Value;
    #endregion

    #region ToInt64 conversions
    public static long ToInt64(Complex value)
    {
        if (value.Real < long.MinValue) return long.MinValue;
        if (value.Real > long.MaxValue) return long.MaxValue;
        return (long)value.Real; // Ignore imaginary part
    }
    public static long ToInt64(DateTime value) => value.Ticks;
    public static long ToInt64(DateTimeOffset value) => value.Ticks;
    public static long ToInt64(TimeSpan value) => value.Ticks;
    #endregion

    #region IntPtr (nint) conversions
    public static nint ToIntPtr(BigInteger value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)(long)value;
    public static nint ToIntPtr(decimal value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(double value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(Half value) => (float)value < nint.MinValue ? nint.MinValue : (float)value > nint.MaxValue ? nint.MaxValue : (nint)(float)value;
    public static nint ToIntPtr(long value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(float value) => value < nint.MinValue ? nint.MinValue : value > nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(ulong value) => value > (ulong)nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(nuint value) => value > (nuint)nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(uint value) => value > (uint)nint.MaxValue ? nint.MaxValue : (nint)value;
    public static nint ToIntPtr(bool value) => value ? 1 : (nint)0;
    #endregion

    #region ToSingle conversions
    public static float ToSingle(Complex value)
    {
        if (value.Real < float.MinValue) return float.MinValue;
        if (value.Real > float.MaxValue) return float.MaxValue;
        return (float)value.Real; // Ignore imaginary part
    }
    #endregion

    #region ToTimeSpan conversions
    public static TimeSpan ToTimeSpan(TimeOnly value) => value.ToTimeSpan();
    #endregion

    #region ToUInt32 conversions
    public static uint ToUInt32(Rune value) => value.Value > uint.MaxValue ? uint.MaxValue : (uint)value.Value;
    #endregion

    #region UIntPtr (nuint) conversions
    public static nuint ToUIntPtr(BigInteger value) => value < 0 ? 0 : value > (BigInteger)nuint.MaxValue ? nuint.MaxValue : (nuint)(ulong)value;
    public static nuint ToUIntPtr(decimal value) => value < 0 ? 0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    public static nuint ToUIntPtr(double value) => value < 0 ? 0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    public static nuint ToUIntPtr(Half value) => (float)value < 0 ? 0 : (float)value > nuint.MaxValue ? nuint.MaxValue : (nuint)(float)value;
    public static nuint ToUIntPtr(int value) => value < 0 ? 0 : (nuint)value;
    public static nuint ToUIntPtr(long value) => value < 0 ? 0 : value > (long)nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    public static nuint ToUIntPtr(nint value) => value < 0 ? 0 : (nuint)value;
    public static nuint ToUIntPtr(sbyte value) => value < 0 ? 0 : (nuint)value;
    public static nuint ToUIntPtr(float value) => value < 0 ? 0 : value > nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    public static nuint ToUIntPtr(ulong value) => value > nuint.MaxValue ? nuint.MaxValue : (nuint)value;
    public static nuint ToUIntPtr(bool value) => value ? 1 : (nuint)0;

    #endregion
}