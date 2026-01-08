using IvoEngine.Expressions.ExpressionUtils;
using IvoEngine.Expressions.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IvoEngine.Expressions.Mapping
{
    //public class PropertyMapping
    //{
    //    public string SourceName { get; set; } = string.Empty;
    //    public string TargetName { get; set; } = string.Empty;

    //    public bool Equals(PropertyMapping? other)
    //    {
    //        if (other is null)
    //        {
    //            return false;
    //        }
    //        if (ReferenceEquals(this, other))
    //        {
    //            return true;
    //        }
    //        return SourceName == other.SourceName && TargetName == other.TargetName;
    //    }

    //    public override bool Equals(object? obj)
    //    {
    //        return Equals(obj as PropertyMapping);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return HashCode.Combine(SourceName, TargetName);
    //    }

    //    public static bool operator ==(PropertyMapping? left, PropertyMapping? right)
    //    {
    //        if(left is null)
    //        {
    //            return right is null;
    //        }

    //        return left.Equals(right);
    //    }

    //    public static bool operator !=(PropertyMapping? left, PropertyMapping? right)
    //    {
    //        return !(left == right);
    //    }
    //}

    /// <summary>
    /// Abstract base class for all types of property mappings
    /// </summary>
    public abstract class PropertyMapping
    {
        /// <summary>
        /// Name of the target property to map to
        /// </summary>
        public string TargetName { get; set; } = string.Empty;

        /// <summary>
        /// Mapping description (optional)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Abstract method to get the mapping expression
        /// </summary>
        /// <param name="sourceExpression">Expression representing the source object</param>
        /// <param name="sourceType">Type of the source object</param>
        /// <param name="targetType">Type of the target object</param>
        /// <returns>Expression to get the value for the target property</returns>
        public abstract Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType);

        /// <summary>
        /// Method for mapping validation
        /// </summary>
        /// <param name="sourceType">Type of the source object</param>
        /// <param name="targetType">Type of the target object</param>
        /// <returns>True if the mapping is valid</returns>
        public virtual bool Validate(Type sourceType, Type targetType)
        {
            var targetProperty = targetType.GetProperty(TargetName);
            return targetProperty != null && targetProperty.CanWrite;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PropertyMapping other)
            {
                return TargetName == other.TargetName;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return TargetName.GetHashCode();
        }
    }

    /// <summary>
    /// Mapping from one source property to target property
    /// </summary>
    public class SimplePropertyMapping : PropertyMapping
    {
        /// <summary>
        /// Name of the source property
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        public override Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
        {
            var sourceProperty = sourceType.GetProperty(SourceName);
            if (sourceProperty == null)
            {
                throw new InvalidOperationException($"Property '{SourceName}' not found in type '{sourceType.Name}'");
            }

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
            {
                throw new InvalidOperationException($"Property '{TargetName}' not found in type '{targetType.Name}'");
            }

            var sourceValue = Expression.Property(sourceExpression, sourceProperty);

            // If the types are different, perform conversion
            if (sourceProperty.PropertyType != targetProperty.PropertyType)
            {
                return ConversionUtils.ConversionExpression(sourceValue, targetProperty.PropertyType);
            }

            return sourceValue;
        }

        public override bool Validate(Type sourceType, Type targetType)
        {
            if (!base.Validate(sourceType, targetType))
                return false;

            var sourceProperty = sourceType.GetProperty(SourceName);
            if (sourceProperty == null || !sourceProperty.CanRead)
                return false;

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
                return false;

            return ConversionHelper.IsTypeConvertible(sourceProperty.PropertyType, targetProperty.PropertyType);
        }
    }

    /// <summary>
    /// Mapping of a constant value to target property
    /// </summary>
    public class ConstantValueMapping : PropertyMapping
    {
        /// <summary>
        /// Constant value to be assigned to the target property
        /// </summary>
        public object? Value { get; set; }

        public override Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
        {
            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
            {
                throw new InvalidOperationException($"Property '{TargetName}' not found in type '{targetType.Name}'");
            }

            // Create a constant expression with the given value
            var constantExpression = Expression.Constant(Value);

            // If conversion is needed, perform it
            if (Value != null && Value.GetType() != targetProperty.PropertyType)
            {
                return ConversionUtils.ConversionExpression(constantExpression, targetProperty.PropertyType);
            }

            return constantExpression;
        }

        public override bool Validate(Type sourceType, Type targetType)
        {
            if (!base.Validate(sourceType, targetType))
                return false;

            if (Value == null)
                return true;

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
                return false;

            return ConversionHelper.IsTypeConvertible(Value.GetType(), targetProperty.PropertyType);
        }
    }

    /// <summary>
    /// Mapping using a function/method
    /// </summary>
    public class FunctionMapping : PropertyMapping
    {
        /// <summary>
        /// Lambda expression or delegate to be called to get the value
        /// </summary>
        public LambdaExpression? Function { get; set; }

        public override Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
        {
            if (Function == null)
            {
                throw new InvalidOperationException("Function is not set for FunctionMapping");
            }

            // Create a function call passing the source object as a parameter
            var functionCall = Expression.Invoke(Function, sourceExpression);

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
            {
                throw new InvalidOperationException($"Property '{TargetName}' not found in type '{targetType.Name}'");
            }

            // If the function result is not of the correct type, convert it
            if (functionCall.Type != targetProperty.PropertyType)
            {
                return ConversionUtils.ConversionExpression(functionCall, targetProperty.PropertyType);
            }

            return functionCall;
        }

        public override bool Validate(Type sourceType, Type targetType)
        {
            if (!base.Validate(sourceType, targetType))
                return false;

            if (Function == null)
                return false;

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
                return false;

            // Check if the function accepts the correct parameter type
            if (Function.Parameters.Count != 1)
                return false;

            if (!sourceType.IsAssignableFrom(Function.Parameters[0].Type))
                return false;

            // Check if the function return type is convertible to the target type
            return ConversionHelper.IsTypeConvertible(Function.ReturnType, targetProperty.PropertyType);
        }
    }

    /// <summary>
    /// Hierarchical mapping for complex objects
    /// </summary>
    public class NestedPropertyMapping : PropertyMapping
    {
        /// <summary>
        /// Name of the source property containing the nested object
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Mappings for properties of the nested object
        /// </summary>
        public IList<PropertyMapping> NestedMappings { get; set; } = new List<PropertyMapping>();

        public override Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
        {
            var sourceProperty = sourceType.GetProperty(SourceName);
            if (sourceProperty == null)
            {
                throw new InvalidOperationException($"Property '{SourceName}' not found in type '{sourceType.Name}'");
            }

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
            {
                throw new InvalidOperationException($"Property '{TargetName}' not found in type '{targetType.Name}'");
            }

            var sourceNestedObject = Expression.Property(sourceExpression, sourceProperty);
            var targetNestedType = targetProperty.PropertyType;

            // Create an expression to create a new object of the target nested type
            var targetNestedVar = Expression.Variable(targetNestedType, "nested");
            var expressions = new List<Expression>();

            // Create a new instance of the nested object
            expressions.Add(Expression.Assign(targetNestedVar, Expression.New(targetNestedType)));

            // Apply nested mappings
            foreach (var nestedMapping in NestedMappings)
            {
                var nestedTargetProperty = targetNestedType.GetProperty(nestedMapping.TargetName);
                if (nestedTargetProperty != null && nestedTargetProperty.CanWrite)
                {
                    var nestedMappingExpr = nestedMapping.GetMappingExpression(sourceNestedObject, sourceProperty.PropertyType, targetNestedType);
                    expressions.Add(Expression.Assign(Expression.Property(targetNestedVar, nestedTargetProperty), nestedMappingExpr));
                }
            }

            // Return the created nested object
            expressions.Add(targetNestedVar);

            return Expression.Block(new[] { targetNestedVar }, expressions);
        }

        public override bool Validate(Type sourceType, Type targetType)
        {
            if (!base.Validate(sourceType, targetType))
                return false;

            var sourceProperty = sourceType.GetProperty(SourceName);
            if (sourceProperty == null || !sourceProperty.CanRead)
                return false;

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
                return false;

            // Validate all nested mappings
            foreach (var nestedMapping in NestedMappings)
            {
                if (!nestedMapping.Validate(sourceProperty.PropertyType, targetProperty.PropertyType))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Mapping from multiple source properties to one target property
    /// </summary>
    public class MultiSourceMapping : PropertyMapping
    {
        /// <summary>
        /// List of source property names
        /// </summary>
        public IList<string> SourceNames { get; set; } = new List<string>();

        /// <summary>
        /// Lambda expression combining source property values
        /// </summary>
        public LambdaExpression? CombineFunction { get; set; }

        public override Expression GetMappingExpression(Expression sourceExpression, Type sourceType, Type targetType)
        {
            if (CombineFunction == null)
            {
                throw new InvalidOperationException("CombineFunction is not set for MultiSourceMapping");
            }

            var sourceValues = new List<Expression>();

            // Get values of all source properties
            foreach (var sourceName in SourceNames)
            {
                var sourceProperty = sourceType.GetProperty(sourceName);
                if (sourceProperty == null)
                {
                    throw new InvalidOperationException($"Property '{sourceName}' not found in type '{sourceType.Name}'");
                }

                sourceValues.Add(Expression.Property(sourceExpression, sourceProperty));
            }

            // Create a call to the combination function with source property values
            var functionCall = Expression.Invoke(CombineFunction, sourceValues.ToArray());

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
            {
                throw new InvalidOperationException($"Property '{TargetName}' not found in type '{targetType.Name}'");
            }

            // Convert the result to the target type if needed
            if (functionCall.Type != targetProperty.PropertyType)
            {
                return ConversionUtils.ConversionExpression(functionCall, targetProperty.PropertyType);
            }

            return functionCall;
        }

        public override bool Validate(Type sourceType, Type targetType)
        {
            if (!base.Validate(sourceType, targetType))
                return false;

            if (CombineFunction == null)
                return false;

            if (SourceNames.Count == 0)
                return false;

            // Check existence of all source properties
            foreach (var sourceName in SourceNames)
            {
                var sourceProperty = sourceType.GetProperty(sourceName);
                if (sourceProperty == null || !sourceProperty.CanRead)
                    return false;
            }

            var targetProperty = targetType.GetProperty(TargetName);
            if (targetProperty == null)
                return false;

            // Check if the function accepts the correct number of parameters
            if (CombineFunction.Parameters.Count != SourceNames.Count)
                return false;

            // Check the function return type
            return ConversionHelper.IsTypeConvertible(CombineFunction.ReturnType, targetProperty.PropertyType);
        }
    }

}
