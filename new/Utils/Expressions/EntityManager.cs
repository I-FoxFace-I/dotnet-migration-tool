using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Threading;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using IvoEngine.Expressions.Factories;
using IvoEngine.Expressions.Helpers;

namespace IvoEngine.Expressions;

/// <summary>
/// Provides universal tools for working with entities of type T.
/// Offers functions for creating, cloning, comparing, and updating entity instances.
/// </summary>
/// <typeparam name="T">Type of entity to work with</typeparam>
public static class EntityManager<T>
{
    private static readonly Type _targetType = TypeHelper.GetTargetType<T>();
    private static readonly Lazy<Func<T>> _defaultFunction = new Lazy<Func<T>>(() => StandartMethodsFactory.CreateDefaultFunction<T>(_targetType));
    private static readonly Lazy<Func<T, T>> _cloneFunction = new Lazy<Func<T, T>>(() => StandartMethodsFactory.CreateCloneFunction<T>(_targetType));
    private static readonly Lazy<Action<T, T>> _updateAction = new Lazy<Action<T, T>>(() => StandartMethodsFactory.CreateUpdateAction<T>(_targetType));
    private static readonly Lazy<Func<T, T, T>> _updateFunction = new Lazy<Func<T, T, T>>(() => StandartMethodsFactory.CreateUpdateFunction<T>(_targetType));
    private static readonly Lazy<Func<T, T, bool>> _compareFunction = new Lazy<Func<T, T, bool>>(() => StandartMethodsFactory.CreateCompareFunction<T>(_targetType));

    /// <summary>
    /// Type for which the supported entity manager functions work
    /// For Nullable<T> we get the inner type T
    /// </summary>
    private static Type TargetType => _targetType;

    /// <summary>
    /// List of all properties contained in the given data type
    /// Used for quick verification of property existence
    /// </summary>
    private static HashSet<string> PropertyNames => _targetType.GetProperties()?.Select(x => x.Name).ToHashSet() ?? new();

    /// <summary>
    /// Function to create a new instance of the respective data type respecting required properties
    /// For required properties, default values are set
    /// </summary>
    private static Func<T> DefaultFunction => _defaultFunction.Value;

    /// <summary>
    /// Function for universal cloning of data type instances as a deepcopy
    /// Creates a new instance and copies all property values into it
    /// </summary>
    private static Func<T, T> CloneFunction => _cloneFunction.Value;

    /// <summary>
    /// Function for overwriting values from the source instance to the target
    /// Copies all property values from the source object to the target
    /// </summary>
    private static Action<T,T> UpdateAction => _updateAction.Value;

    /// <summary>
    /// Function for overwriting values from the source instance to the target
    /// Copies all property values from the source object to the target
    /// </summary>
    private static Func<T, T, T> UpdateFunction => StandartMethodsFactory.CreateUpdateFunction<T>(TargetType);

    /// <summary>
    /// Function for comparing 2 instances of the given data type, using recursive property comparison
    /// For reference types, recursively compares their properties
    /// </summary>
    private static Func<T, T, bool> CompareFunction => _compareFunction.Value;

    /// <summary>
    /// Creates a new instance of type T with properties set to default/empty values.
    /// </summary>
    public static T Empty => DefaultFunction();

    /// <summary>
    /// Provides a universal tool for cloning instances of type T. The method creates a new instance
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
        return CloneFunction(source);
    }

    /// <summary>
    /// Compares two instances of type T. Returns True if all properties of the given instances are equal (by value),
    /// otherwise returns False. If one of the given instances is null, returns True if the other is also null, otherwise
    /// returns False. The method uses recursion to compare the properties of object types (with their own properties) and
    /// for properties of collection types, compares all elements according to their order in the sequence.
    /// </summary>
    /// <param name="lhs">Instance T or null</param>
    /// <param name="rhs">Instance T or null</param>
    /// <returns>Comparison result, which is True when all properties of both objects are equal</returns>
    public static bool Compare(T? lhs, T? rhs)
    {
        // If the first object is null, return True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, return False
        if (rhs is null)
        {
            return false;
        }
        // Comparison using the prepared function for recursive property comparison
        return CompareFunction(lhs, rhs);
    }

    /// <summary>
    /// Provides a universal update method for an instance of type T. For the target item, sets all its properties
    /// to values corresponding to the properties of the source object. If the target item is null, an exception is throwen. 
    /// When the source item is null, the target object is not updated at all.
    /// </summary>
    /// <param name="target">Instance T or null</param>
    /// <param name="source">Instance T or null</param>
    /// <returns>Updated instance or null</returns>
    public static void Update(T target, T? source)
    {
        if (target is null)
            throw new ArgumentNullException("target");
        if (source is null)
            return;

        UpdateAction(target, source);
    }

    /// <summary>
    /// Provides a universal update procedure for an instance of type T. For the target item, sets all its properties
    /// to values corresponding to the properties of the source object. If the target item is null, it is set as a clone
    /// of the source item. When the source item is null, the target object is not updated at all.
    /// </summary>
    /// <param name="target">Instance T or null</param>
    /// <param name="source">Instance T or null</param>
    /// <returns>Updated instance or null</returns>
    public static T? Modify(T? target, T? source)
    {
        // If the source object is null, leave the target object unchanged
        if (source is null)
        {
            return target;
        }
        // If the target object is null, create an empty instance
        if (target is null)
        {
            return Empty;
        }
        // Update all properties, including inherited ones
        return UpdateFunction(target, source);
    }

    

    // Function for overwriting values that are declared only within the class (ignores properties inherited from the parent class)
    // Copies only the values of properties that are directly declared in the class
    private static readonly Action<T, T> UpdateDeclaredFunction = DeclaredMethodsFactory.CreateUpdateDeclaredFunction<T>(TargetType);

    // Function for comparing 2 instances, comparing only declared properties within the class (ignores properties that are inherited)
    // Compares only the properties that are directly declared in the class
    private static readonly Func<T, T, bool> CompareDeclaredFunction = DeclaredMethodsFactory.CreateCompareDeclaredFunction<T>(TargetType);

    // Function that performs a deepcopy of the object by setting property values to the value of the origin, except for those listed in the function parameters where it leaves the default value
    // For excluded properties, uses default values
    private static readonly Func<T, List<string>, T> ExclusiveCloneFunction = ExclusiveMethodsFactory.CreateCloneExclusiveFunction<T>(TargetType);

    // Function that overwrites all values from the source object to the target object, except for the values listed in the function parameters
    // Excluded properties remain unchanged
    private static readonly Action<T, T, List<string>> ExclusiveUpdateFunction = ExclusiveMethodsFactory.CreateExclusiveUpdateFunction<T>(TargetType);

    // Function that compares all properties using recursive value comparison, except for those listed in the function parameters where it leaves the original value
    // Excluded properties are ignored during comparison
    private static readonly Func<T, T, List<string>, bool> ExclusiveCompareFunction = ExclusiveMethodsFactory.CreateExclusiveCompareFunction<T>(TargetType);

    // Dictionary of functions for updating individual properties by their names
    // Allows updating individual properties separately
    private static readonly Dictionary<string, Action<T, T>> PropertyUdaters = StandartMethodsFactory.CreatePropertyUpdateFunctions<T>(TargetType);

    // Dictionary of functions for comparing individual properties by their names
    // Allows comparing individual properties separately
    private static readonly Dictionary<string, Func<T, T, bool>> PropertyComparers = StandartMethodsFactory.CreatePropertyCompareFunctions<T>(TargetType);

    // Function for calculating the hash code of an instance
    // Used for efficient storage in hashed collections
    //private static readonly Func<T, int> GetHashMethod = StandartMethodsFactory.CreateHashingMethod<T>();

    

    /// <summary>
    /// Compares two instances of type T, but ignores inheritance. Returns True if for the given instances all properties
    /// that are not inherited are equal (by value), otherwise returns False. If one of the given instances is null,
    /// returns True if the other is also null, otherwise returns False. The method uses recursion to compare the properties
    /// of object types (with their own properties) and for properties of collection types, compares all elements according to their order in the sequence.
    /// </summary>
    /// <param name="lhs">Instance T or null</param>
    /// <param name="rhs">Instance T or null</param>
    /// <returns>Comparison result, which is True when all declared properties of both objects are equal</returns>
    public static bool CompareDeclaredOnly(T? lhs, T? rhs)
    {
        // If the first object is null, return True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, return False
        if (rhs is null)
        {
            return false;
        }
        // Comparison using only declared properties (not inherited)
        return CompareDeclaredFunction(lhs, rhs);
    }

    /// <summary>
    /// Provides a universal update procedure for an instance of type T, but ignores inheritance. For the target item,
    /// sets all its declared properties to values corresponding to the properties of the source object. If the target item
    /// is null, it is set as a clone of the source item and properties inherited from the base class are set to
    /// default/empty values. When the source item is null, the target object is not updated at all.
    /// </summary>
    /// <param name="origin">Instance T or null</param>
    /// <param name="source">Instance T or null</param>
    /// <returns>Updated instance or null</returns>
    public static T? UpdateDeclaredOnly(T? origin, T? source)
    {
        // If the source object is null, leave the target object unchanged
        if (source is null)
        {
            return origin;
        }
        // If the target object is null, create an empty instance
        if (origin is null)
        {
            origin = Empty;
        }
        // Update only declared properties (not inherited)
        UpdateDeclaredFunction(origin, source);

        return origin;
    }

    

    /// <summary>
    /// Compares selected properties of the given instances and returns true if all are equal. If one of the given
    /// instances is null, returns True if the other is also null, otherwise returns False. The method uses recursion to
    /// compare the properties of object types (with their own properties) and for properties of collection types, compares
    /// all elements according to their order in the sequence.
    /// </summary>
    /// <param name="lhs">Instance T or null</param>
    /// <param name="rhs">Instance T or null</param>
    /// <param name="properties">Set of properties to compare</param>
    /// <returns>Comparison result, which is True when all properties from the given set are equal for both objects</returns>
    public static bool CompareProperties(T? lhs, T? rhs, params string[]? properties)
    {
        // If no properties are specified for comparison, return True (nothing to compare)
        if (properties is null || !properties.Any())
        {
            return true;
        }
        // If the first object is null, return True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, return False
        if (rhs is null)
        {
            return false;
        }
        // Comparison using only specified properties
        foreach (var property in properties)
        {
            // Check if the property exists in the given type
            if (!PropertyNames.Contains(property))
            {
                continue;
            }
            // If properties do not match, return False immediately
            if (!PropertyComparers[property](lhs, rhs))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Sets all properties of the target object from the provided set to values corresponding to the properties
    /// of the source object. If the target object is null, it is set to an empty instance and its required properties are set.
    /// If the source object is null, the properties of the target object are not affected at all.
    /// </summary>
    /// <param name="origin">Instance T or null</param>
    /// <param name="source">Instance T or null</param>
    /// <param name="properties">Set of properties to update</param>
    /// <returns>Updated instance or null</returns>
    public static T? UpdateProperties(T? origin, T? source, params string[]? properties)
    {
        // If the source object is null, leave the target object unchanged
        if (source is null)
        {
            return origin;
        }
        // If no properties are specified for update, return the target object unchanged
        if (properties is null || !properties.Any())
        {
            return origin;
        }
        // If the target object is null, create an empty instance
        if (origin is null)
        {
            origin = Empty;
        }
        // Update only specified properties
        foreach (var property in properties)
        {
            // Check if the property exists in the given type
            if (!PropertyNames.Contains(property))
            {
                continue;
            }
            // Update specific property
            PropertyUdaters[property](origin, source);
        }

        return origin;
    }

    /// <summary>
    /// Creates a deep copy of the source object, but excludes specified properties from cloning.
    /// For excluded properties, the default value will be used.
    /// </summary>
    /// <param name="source">Source object to clone</param>
    /// <param name="excludeFromClone">Names of properties to exclude from cloning</param>
    /// <returns>Deep copy of the object with excluded properties set to default values</returns>
    public static T CloneExclusive(T source, params string[]? excludeFromClone)
    {
        // If no properties are specified for exclusion, use standard cloning
        if (excludeFromClone == null || !excludeFromClone.Any())
        {
            return Clone(source);
        }
        // Use the specialized function for cloning with excluded properties
        return ExclusiveCloneFunction(source, excludeFromClone.ToList());
    }

    /// <summary>
    /// Compares two instances, but ignores specified properties during comparison.
    /// </summary>
    /// <param name="lhs">First compared instance</param>
    /// <param name="rhs">Second compared instance</param>
    /// <param name="excludeFromCompare">Names of properties to exclude from comparison</param>
    /// <returns>True if all non-excluded properties are equal, otherwise False</returns>
    public static bool CompareExclusive(T? lhs, T? rhs, params string[]? excludeFromCompare)
    {
        // If no properties are specified for exclusion, use standard comparison
        if (excludeFromCompare is null || !excludeFromCompare.Any())
        {
            return Compare(lhs, rhs);
        }
        // If the first object is null, return True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, return False
        if (rhs is null)
        {
            return false;
        }
        // Use the specialized function for comparison with excluded properties
        return ExclusiveCompareFunction(lhs, rhs, excludeFromCompare.ToList());
    }

    /// <summary>
    /// Updates the target object using the source object, but excludes specified properties from the update.
    /// These properties remain unchanged.
    /// </summary>
    /// <param name="origin">Target object to be updated</param>
    /// <param name="source">Source object from which values will be taken</param>
    /// <param name="excludeFromUpdate">Names of properties to exclude from update</param>
    /// <returns>Updated instance or null</returns>
    public static T? UpdateExclusive(T? origin, T? source, params string[]? excludeFromUpdate)
    {
        // If no properties are specified for exclusion, use standard update
        if (excludeFromUpdate is null || !excludeFromUpdate.Any())
        {
            return Modify(origin, source);
        }
        // If the source object is null, leave the target object unchanged
        if (source is null)
        {
            return origin;
        }
        // If the target object is null, create an empty instance
        if (origin is null)
        {
            origin = Empty;
        }
        // Use the specialized function for update with excluded properties
        ExclusiveUpdateFunction(origin, source, excludeFromUpdate.ToList());

        return origin;
    }

    /// <summary>
    /// Compares two instances as table data. Used for comparing objects in the context of database tables.
    /// </summary>
    /// <param name="lhs">First compared instance</param>
    /// <param name="rhs">Second compared instance</param>
    /// <returns>True if the instances are equal, otherwise False</returns>
    public static bool CompareTable(T lhs, T rhs)
    {
        // If the first object is null, return True only if the second is also null
        if (lhs is null)
        {
            return rhs is null;
        }
        // If the second object is null and the first is not, return False
        if (rhs is null)
        {
            return false;
        }
        // Uses the same function as Compare, but with a different name for semantic clarity in the context of tables
        return CompareFunction(lhs, rhs);
    }

    /// <summary>
    /// Updates an instance in the context of table data. Used for updating objects in the context of database tables.
    /// </summary>
    /// <param name="origin">Target object to update</param>
    /// <param name="source">Source object with new values</param>
    public static void UpdateTable(T origin, T source)
    {
        // Null parameter check - if any is null, the method cannot update anything
        if (source is null)
        {
            return;
        }
        if (origin is null)
        {
            return;
        }
        // Uses the same function as Update, but with a different name for semantic clarity in the context of tables
        UpdateFunction(origin, source);
    }

    /// <summary>
    /// Gets the hash code for the given instance. Used for efficient storage in hash collections.
    /// </summary>
    /// <param name="item">Instance for which to get the hash code</param>
    /// <param name="includeProperties">Properties to include in the hash code calculation (null for all)</param>
    /// <param name="excludeProperties">Properties to exclude from the hash code calculation</param>
    /// <returns>Hash code of the instance</returns>
    public static int GetHashCode(T item, IEnumerable<string>? includeProperties = null, IEnumerable<string>? excludeProperties = null)
    {
        //if (item == null)
        //{
        //    return 0;
        //}
        //return GetHashMethod(item);

        return 0;
    }
}
