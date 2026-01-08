using Utils.Expressions.Helpers;
using Utils.Expressions.StaticData;
using Utils.Extensions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Utils.Expressions.ExpressionUtils;

/// <summary>
/// Provides helper methods for creating and manipulating expressions for entity properties.
/// </summary>
public static class PropertyUtils
{

    /// <summary>
    /// Creates an expression for comparing values of two properties.
    /// </summary>
    /// <param name="obj1Prop">Expression representing the first property</param>
    /// <param name="obj2Prop">Expression representing the second property</param>
    /// <returns>Expression comparing both properties</returns>
    public static Expression ComparisonExpression(Expression obj1Prop, Expression obj2Prop)
    {
        Type type = obj1Prop.Type;

        // If the type is nullable, get the underlying type
        if (TypeHelper.IsNullable(type, out var uType) && uType is Type underlyingType)
        {
            type = underlyingType;
        }

        // For primitive and system types, use simple comparison
        if (TypeHelper.IsPrimitiveType(type))
        {
            return Expression.Equal(obj1Prop, obj2Prop);
        }
        else if(TypeHelper.IsCollectionType(type))
        {
            return MethodUtils.CompareCollections(type, obj1Prop, obj2Prop);
        }
        else if (type.IsClass)
        {
            // For classes, use EntityManagerNew.Compare recursively
            var compareMethod = typeof(EntityManager<>).MakeGenericType(type)!.GetMethod("Compare")!;

            return Expression.Call(null, compareMethod, obj1Prop, obj2Prop);
        }

        // For other types, use standard comparison
        return Expression.Equal(obj1Prop, obj2Prop);
    }

    /// <summary>
    /// Creates an expression for updating the value of a target property using a source property.
    /// </summary>
    /// <param name="originItem">Expression representing the target property</param>
    /// <param name="updateSource">Expression representing the source property</param>
    /// <param name="nullableProperty">Determines whether the property is nullable</param>
    /// <param name="defaultValue">Expression representing the default value to use in case of null</param>
    /// <returns>Expression updating the target property</returns>
    public static Expression UpdateExpression(Expression originItem, Expression updateSource, bool nullableProperty, Expression defaultValue)
    {
        var type = originItem.Type;

        // For nullable properties of primitive and system types
        if (nullableProperty)
        {
            if (TypeHelper.IsPrimitiveType(type))
            {
                return Expression.Assign(originItem, updateSource);
            }
            else if (type.IsValueType)
            {
                return Expression.Assign(originItem, updateSource);
            }
        }

        

        // For primitive and system types, use simple assignment
        if (TypeHelper.IsPrimitiveType(type))
        {
            return Expression.Assign(originItem, updateSource);
        }
        else if (TypeHelper.IsNullable(type, out var underlyingType) && TypeHelper.IsPrimitiveType(underlyingType ?? typeof(void)))
        {
            return Expression.Assign(originItem, updateSource);
        }
        else if (TypeHelper.IsCollectionType(type))
        {
            return MethodUtils.UpdateCollection(type, originItem, updateSource, nullableProperty, defaultValue);
        }
        else if (type.IsClass)
        {
            // For classes, use EntityManagerNew.Clone and EntityManagerNew.Update
            var cloneMethod = TypesInfo.EntityManagerType.MakeGenericType(type)!.GetMethod("Clone")!;
            var updateMethod = TypesInfo.EntityManagerType.MakeGenericType(type)!.GetMethod("Update")!;

            // For nullable properties, check if the source value is not null
            var nullSource = Expression.Equal(updateSource, Expression.Constant(null));
            // Check if the target instance is not null
            var nullOrigin = Expression.Equal(originItem, Expression.Constant(null));

            // Create a clone of the source instance
            var cloneInstance = Expression.Assign(originItem, Expression.Call(null, cloneMethod, updateSource));

            // Update the target instance using the source clone
            var propertyUpdate = Expression.Assign(originItem, Expression.Call(null, updateMethod, originItem, cloneInstance));
            
            return Expression.IfThenElse(
                nullSource,
                Expression.Assign(originItem, defaultValue),
                Expression.IfThenElse(nullOrigin, defaultValue, propertyUpdate)
            );
        }
        else
        {
            if (nullableProperty)
            {
                // For nullable properties, check if the source value is not null
                var nullSource = Expression.Equal(updateSource, Expression.Constant(null));

                return Expression.IfThenElse(
                    nullSource,
                    Expression.Assign(originItem, defaultValue),
                    Expression.Assign(originItem, updateSource)
                );
            }
            else
            {
                // For other types, use simple assignment
                return Expression.Assign(originItem, updateSource);
            }
        }
    }
}
