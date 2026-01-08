using Utils.Expressions.Conversion;
using System.Numerics;
using System.Text;

namespace Utils.Expressions.StaticData;



public static class ConversionsInfo
{
    /// <summary>
    /// Indexes assigned to individual primitive types for faster orientation in the matrix
    /// </summary>
    private static readonly Dictionary<Type, int> _primitiveTypeIndexes = CreateTypeIndexDictionary();

    /// <summary>
    /// Dictionary containing all safe (implicit) conversions from one type to other types
    /// </summary>
    private static readonly Dictionary<Type, ISet<Type>> _implicitConversions = new Dictionary<Type, ISet<Type>>
    {
        { typeof(bool), new HashSet<Type> { typeof(bool), typeof(string) } },
        { typeof(byte), new HashSet<Type> { typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(nint), typeof(nuint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(sbyte), new HashSet<Type> { typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(nint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(char), new HashSet<Type> { typeof(char), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(nint), typeof(nuint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger) } },
        { typeof(decimal), new HashSet<Type> { typeof(decimal), typeof(string) } },
        { typeof(double), new HashSet<Type> { typeof(double), typeof(string), typeof(Complex) } },
        { typeof(float), new HashSet<Type> { typeof(float), typeof(double), typeof(string), typeof(Complex) } },
        { typeof(int), new HashSet<Type> { typeof(int), typeof(long), typeof(nint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(Index), typeof(BigInteger), typeof(Complex) } },
        { typeof(uint), new HashSet<Type> { typeof(uint), typeof(long), typeof(ulong), typeof(nuint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(nint), new HashSet<Type> { typeof(nint), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger) } },
        { typeof(nuint), new HashSet<Type> { typeof(nuint), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger) } },
        { typeof(long), new HashSet<Type> { typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(TimeSpan), typeof(BigInteger), typeof(Complex) } },
        { typeof(ulong), new HashSet<Type> { typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(short), new HashSet<Type> { typeof(short), typeof(int), typeof(long), typeof(nint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(ushort), new HashSet<Type> { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(nint), typeof(nuint), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(BigInteger), typeof(Complex) } },
        { typeof(string), new HashSet<Type> { typeof(string) } },
        { typeof(DateTime), new HashSet<Type> { typeof(DateTime), typeof(string) } },
        { typeof(DateTimeOffset), new HashSet<Type> { typeof(DateTimeOffset), typeof(string) } },
        { typeof(Guid), new HashSet<Type> { typeof(Guid), typeof(string) } },
        { typeof(Index), new HashSet<Type> { typeof(Index), typeof(string) } },
        { typeof(TimeSpan), new HashSet<Type> { typeof(TimeSpan), typeof(string) } },
        { typeof(Version), new HashSet<Type> { typeof(Version), typeof(string) } },
        { typeof(Type), new HashSet<Type> { typeof(Type), typeof(string) } },
        { typeof(Uri), new HashSet<Type> { typeof(Uri), typeof(string) } },
        { typeof(TimeOnly), new HashSet<Type> { typeof(TimeOnly), typeof(string) } },
        { typeof(DateOnly), new HashSet<Type> { typeof(DateOnly), typeof(string) } },
        { typeof(Half), new HashSet<Type> { typeof(Half), typeof(float), typeof(double), typeof(string) } },
        { typeof(BigInteger) , new HashSet<Type> { typeof(BigInteger), typeof(string) } },
        { typeof(Complex) , new HashSet<Type> { typeof(Complex), typeof(string) } },
        { typeof(Range) , new HashSet<Type> { typeof(Range), typeof(string) } },
        { typeof(Rune) , new HashSet<Type> {  typeof(Rune), typeof(string) } }
    };

    /// <summary>
    /// Dictionary containing all potentially unsafe (explicit) conversions from one type to other types
    /// </summary>
    private static readonly Dictionary<Type, ISet<Type>> _explicitConversions = new Dictionary<Type, ISet<Type>>
    {
        { typeof(bool), new HashSet<Type>() { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(Half), typeof(BigInteger), typeof(nint), typeof(nuint) } },
        { typeof(byte), new HashSet<Type> { typeof(sbyte), typeof(char), typeof(bool), typeof(Half) } },
        { typeof(sbyte), new HashSet<Type> { typeof(byte), typeof(char), typeof(bool), typeof(ushort), typeof(uint), typeof(ulong), typeof(nuint), typeof(Half) } },
        { typeof(char), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(bool), typeof(short), typeof(Rune) } },
        { typeof(decimal), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(Half), typeof(BigInteger), typeof(Complex)} },
        { typeof(double), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(float), typeof(decimal), typeof(Half), typeof(BigInteger) } },
        { typeof(float), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(decimal), typeof(Half), typeof(BigInteger) } },
        { typeof(int), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(uint), typeof(ulong), typeof(nuint), typeof(Half), typeof(Rune) } },
        { typeof(uint), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(nint),typeof(Half), typeof(Rune) } },
        { typeof(nint), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nuint), typeof(ulong) } },
        { typeof(nuint), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint) } },
        { typeof(long), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(ulong), typeof(DateTime), typeof(Half) } },
        { typeof(ulong), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(Half) } },
        { typeof(short), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(ushort), typeof(uint), typeof(ulong), typeof(Half) } },
        { typeof(ushort), new HashSet<Type> { typeof(byte), typeof(sbyte), typeof(char), typeof(bool), typeof(short), typeof(Half) } },
        { typeof(string), new HashSet<Type> { typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid), typeof(TimeSpan), typeof(Version), typeof(Uri), typeof(TimeOnly), typeof(DateOnly), typeof(Half), typeof(BigInteger), typeof(Complex), typeof(Rune), typeof(Range) } },
        { typeof(DateTime), new HashSet<Type> { typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(long) } },
        { typeof(DateTimeOffset), new HashSet<Type> { typeof(DateTime), typeof(DateOnly), typeof(long) } },
        { typeof(Guid), new HashSet<Type>() },
        { typeof(Index), new HashSet<Type> { typeof(int) } },
        { typeof(TimeSpan), new HashSet<Type> { typeof(TimeOnly), typeof(long) } },
        { typeof(Version), new HashSet<Type>() },
        { typeof(Type), new HashSet<Type>() },
        { typeof(Uri), new HashSet<Type>() },
        { typeof(TimeOnly), new HashSet<Type> { typeof(TimeSpan) } },
        { typeof(DateOnly), new HashSet<Type> { typeof(DateTime), typeof(DateTimeOffset) } },
        { typeof(Half) , new HashSet<Type> { typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(decimal) } },
        { typeof(BigInteger) , new HashSet<Type> { typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(nint), typeof(nuint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
        { typeof(Complex) , new HashSet<Type> { typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(long) } },
        { typeof(Range), new HashSet<Type> { } },
        { typeof(Rune), new HashSet<Type> { typeof(char), typeof(int), typeof(uint) } },
    };

    private static readonly ConversionType[,] _conversionMatrix = CreateConversionMartix();

    private static ConversionType[,] CreateConversionMartix()
    {
        var size = _primitiveTypeIndexes.Count;

        var conversionMatrix = new ConversionType[size, size];

        foreach (var sourceItem in _primitiveTypeIndexes)
        {
            var sourceType = sourceItem.Key;
            var sourceIndex = sourceItem.Value;
            var implicitConversions = _implicitConversions[sourceType];
            var explicitConversions = _explicitConversions[sourceType];
            foreach (var targetItem in _primitiveTypeIndexes)
            {
                var targetType = targetItem.Key;
                var targetIndex = targetItem.Value;

                if (implicitConversions.Contains(targetType))
                {
                    conversionMatrix[sourceIndex, targetIndex] = ConversionType.Implicit;
                }
                else if (explicitConversions.Contains(targetType))
                {
                    conversionMatrix[sourceIndex, targetIndex] = ConversionType.Explicit;
                }
                else
                {
                    conversionMatrix[sourceIndex, targetIndex] = ConversionType.NotPossible;
                }
            }
        }

        return conversionMatrix;
    }

    private static Dictionary<Type, int> CreateTypeIndexDictionary()
    {
        Dictionary<Type, int> typeIndexDictionary = new Dictionary<Type, int>();

        for (int i = 0; i < TypesInfo.PrimitiveTypes.Length; i++)
        {
            typeIndexDictionary.Add(TypesInfo.PrimitiveTypes[i], i);
        }

        return typeIndexDictionary;
    }

    public static ConversionType[,] ConversionMatrix => _conversionMatrix;
    public static Dictionary<Type, int> PrimitiveTypeIndexes => _primitiveTypeIndexes;
    public static Dictionary<Type, ISet<Type>> ImplicitConversions => _implicitConversions;
    public static Dictionary<Type, ISet<Type>> ExplicitConversions => _explicitConversions;

}