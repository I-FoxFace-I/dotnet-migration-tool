using Utils.Expressions.Factories;
using Utils.Expressions.Helpers;

namespace Utils.Expressions;
public static class TypeManager<T>
{
    // Type for which the supported entity manager functions work
    // For Nullable<T>, we get the inner type T
    private static Type TargetType = TypeHelper.GetTargetType<T>();

    // Function to create a new instance of the corresponding data type, respecting required properties
    // For required properties, default values are set
    private static readonly Func<T> DefaultFunc = StandartMethodsFactory.CreateDefaultFunction<T>(TargetType);

    // Function for universal cloning of data type instances as a deep copy
    // Creates a new instance and copies all property values into it
    private static readonly Func<T, T> CloneFunc = StandartMethodsFactory.CreateCloneFunction<T>(TargetType);

    // Function for comparing 2 instances of the given data type, which uses recursive property comparison
    // For reference types, it recursively compares their properties
    private static readonly Func<T, T, bool> CompareFunc = StandartMethodsFactory.CreateCompareFunction<T>(TargetType);

    /// <summary>
    /// Creates a new instance of type T with properties set to default/empty values.
    /// </summary>
    public static T Empty => DefaultFunc();

    /// <summary>
    /// This method provides a universal tool for cloning instances of type T. The method creates a new instance
    /// of type T and sets its properties to values corresponding to the properties of the source object.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <returns>Deep copy of the source object</returns>
    public static T Clone(T source)
    {
        // If the source object is null, create an empty instance
        if (source is null)
        {
            return Empty;
        }
        return CloneFunc(source);
    }

    /// <summary>
    /// Compares two instances of type T. Returns True if all properties of the given instances are equal (by value),
    /// otherwise returns False. If one of the given instances is null, returns True if the other is also null, otherwise
    /// returns False. The method uses recursion to compare properties of object types (with their own properties) and
    /// for properties of collection types, it compares all elements by their order in the sequence.
    /// </summary>
    /// <param name="lhs">Instance T or null</param>
    /// <param name="rhs">Instance T or null</param>
    /// <returns>The result of the comparison, which is True when all properties of both objects are equal</returns>
    public static bool Equals(T? lhs, T? rhs)
    {
        // If the first object is null, returns True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, returns False
        if (rhs is null)
        {
            return false;
        }
        // Comparison using a prepared function for recursive property comparison
        return CompareFunc(lhs, rhs);
    }
}
