using System.Numerics;
using System.Text;

namespace IvoEngine.Expressions.Conversion;

/// <summary>
/// Provides safe implicit conversions between primitive types.
/// Implicit conversions are those that are safe in C# and cannot result in data loss.
/// </summary>
public static class ImplicitConverter
{
    #region ToInt16 conversions (to short)
    public static short ToInt16(byte value) => value;
    public static short ToInt16(sbyte value) => value;
    #endregion

    #region ToInt32 conversions (to int)
    public static int ToInt32(byte value) => value;
    public static int ToInt32(sbyte value) => value;
    public static int ToInt32(short value) => value;
    public static int ToInt32(ushort value) => value;
    public static int ToInt32(char value) => value;
    #endregion

    #region ToInt64 conversions (to long)
    public static long ToInt64(byte value) => value;
    public static long ToInt64(sbyte value) => value;
    public static long ToInt64(short value) => value;
    public static long ToInt64(ushort value) => value;
    public static long ToInt64(int value) => value;
    public static long ToInt64(uint value) => value;
    public static long ToInt64(nint value) => value;
    public static long ToInt64(char value) => value;
    #endregion

    #region ToUInt16 conversions (to ushort)
    public static ushort ToUInt16(byte value) => value;
    public static ushort ToUInt16(char value) => value;
    #endregion

    #region ToUInt32 conversions (to uint)
    public static uint ToUInt32(byte value) => value;
    public static uint ToUInt32(ushort value) => value;
    public static uint ToUInt32(char value) => value;
    #endregion

    #region ToUInt64 conversions (to ulong)
    public static ulong ToUInt64(byte value) => value;
    public static ulong ToUInt64(ushort value) => value;
    public static ulong ToUInt64(uint value) => value;
    public static ulong ToUInt64(nuint value) => value;
    public static ulong ToUInt64(char value) => value;
    #endregion

    #region ToSingle conversions (to float)
    public static float ToSingle(byte value) => value;
    public static float ToSingle(sbyte value) => value;
    public static float ToSingle(short value) => value;
    public static float ToSingle(ushort value) => value;
    public static float ToSingle(int value) => value;
    public static float ToSingle(uint value) => value;
    public static float ToSingle(long value) => value;
    public static float ToSingle(ulong value) => value;
    public static float ToSingle(nint value) => value;
    public static float ToSingle(nuint value) => value;
    public static float ToSingle(char value) => value;
    public static float ToSingle(Half value) => (float)value;
    #endregion

    #region ToDouble conversions (to double)
    public static double ToDouble(byte value) => value;
    public static double ToDouble(sbyte value) => value;
    public static double ToDouble(short value) => value;
    public static double ToDouble(ushort value) => value;
    public static double ToDouble(int value) => value;
    public static double ToDouble(uint value) => value;
    public static double ToDouble(long value) => value;
    public static double ToDouble(ulong value) => value;
    public static double ToDouble(nint value) => value;
    public static double ToDouble(nuint value) => value;
    public static double ToDouble(float value) => value;
    public static double ToDouble(char value) => value;
    public static double ToDouble(Half value) => (float)value;
    #endregion

    #region ToDecimal conversions (to decimal)
    public static decimal ToDecimal(byte value) => value;
    public static decimal ToDecimal(sbyte value) => value;
    public static decimal ToDecimal(short value) => value;
    public static decimal ToDecimal(ushort value) => value;
    public static decimal ToDecimal(int value) => value;
    public static decimal ToDecimal(uint value) => value;
    public static decimal ToDecimal(long value) => value;
    public static decimal ToDecimal(ulong value) => value;
    public static decimal ToDecimal(nint value) => value;
    public static decimal ToDecimal(nuint value) => value;
    public static decimal ToDecimal(char value) => value;
    #endregion

    #region ToIndex conversions (to Index)
    public static Index ToIndex(int value) => new Index(value);
    #endregion

    #region ToComplex conversions (to Complex)
    public static Complex ToComplex(byte value) => new Complex(value, 0);
    public static Complex ToComplex(sbyte value) => new Complex(value, 0);
    public static Complex ToComplex(short value) => new Complex(value, 0);
    public static Complex ToComplex(ushort value) => new Complex(value, 0);
    public static Complex ToComplex(int value) => new Complex(value, 0);
    public static Complex ToComplex(uint value) => new Complex(value, 0);
    public static Complex ToComplex(long value) => new Complex(value, 0);
    public static Complex ToComplex(ulong value) => new Complex(value, 0);
    public static Complex ToComplex(float value) => new Complex(value, 0);
    public static Complex ToComplex(double value) => new Complex(value, 0);
    #endregion

    #region ToBigInteger conversions (to BigInteger)
    public static BigInteger ToBigInteger(byte value) => value;
    public static BigInteger ToBigInteger(sbyte value) => value;
    public static BigInteger ToBigInteger(short value) => value;
    public static BigInteger ToBigInteger(ushort value) => value;
    public static BigInteger ToBigInteger(int value) => value;
    public static BigInteger ToBigInteger(uint value) => value;
    public static BigInteger ToBigInteger(long value) => value;
    public static BigInteger ToBigInteger(ulong value) => value;
    public static BigInteger ToBigInteger(nint value) => value;
    public static BigInteger ToBigInteger(nuint value) => value;
    public static BigInteger ToBigInteger(char value) => value;
    #endregion

    #region ToTimeSpan conversions (to TimeSpan)
    public static TimeSpan ToTimeSpan(long ticks) => new TimeSpan(ticks);
    #endregion

    #region ToIntPtr  conversions (to nint)
    public static nint ToIntPtr (byte value) => value;
    public static nint ToIntPtr (sbyte value) => value;
    public static nint ToIntPtr (short value) => value;
    public static nint ToIntPtr (int value) => value;
    public static nint ToIntPtr (char value) => value;
    public static nint ToIntPtr(ushort value) => value;
    #endregion

    #region ToUIntPtr  conversions (to nuint)
    public static nuint ToUIntPtr(byte value) => value;
    public static nuint ToUIntPtr(char value) => value;
    public static nuint ToUIntPtr(ushort value) => value;
    public static nuint ToUIntPtr(uint value) => value;
    #endregion

}
