using System.Linq.Expressions;
using System.Reflection;

namespace Utils.Expressions.Mapping
{
    /// <summary>
    /// Helper class for creating mappings
    /// </summary>
    public static class PropertyMappingExtensions
    {
        /// <summary>
        /// Creates a simple mapping from source to target
        /// </summary>
        public static SimplePropertyMapping MapToProperty(this string sourceName, string targetName)
        {
            return new SimplePropertyMapping
            {
                SourceName = sourceName,
                TargetName = targetName
            };
        }

        /// <summary>
        /// Creates a constant value mapping
        /// </summary>
        public static ConstantValueMapping MapConstant(this string targetName, object? value)
        {
            return new ConstantValueMapping
            {
                TargetName = targetName,
                Value = value
            };
        }

        /// <summary>
        /// Creates a mapping using a function
        /// </summary>
        public static FunctionMapping MapUsing<TSource, TResult>(this string targetName, Expression<Func<TSource, TResult>> func)
        {
            return new FunctionMapping
            {
                TargetName = targetName,
                Function = func
            };
        }

        /// <summary>
        /// Creates a hierarchical mapping
        /// </summary>
        public static NestedPropertyMapping MapNested(this string sourceName, string targetName, params PropertyMapping[] nestedMappings)
        {
            return new NestedPropertyMapping
            {
                SourceName = sourceName,
                TargetName = targetName,
                NestedMappings = nestedMappings.ToList()
            };
        }

        /// <summary>
        /// Creates a multi-source mapping
        /// </summary>
        public static MultiSourceMapping MapMultiple<TSource, TResult>(
            this string targetName,
            LambdaExpression combineFunction,
            params string[] sourceNames)
        {
            return new MultiSourceMapping
            {
                TargetName = targetName,
                SourceNames = sourceNames.ToList(),
                CombineFunction = combineFunction
            };
        }

        /// <summary>
        /// Creates a hierarchical mapping with a builder for nested mappings
        /// </summary>
        public static NestedPropertyMapping MapNested<TNestedSource, TNestedTarget>(
            this string sourceName,
            string targetName,
            Action<MappingBuilder<TNestedSource, TNestedTarget>> configureNested)
        {
            var builder = new MappingBuilder<TNestedSource, TNestedTarget>();
            configureNested(builder);
            var options = builder.Build();
            var finalMappings = options.CreateFinalMappings();

            return new NestedPropertyMapping
            {
                SourceName = sourceName,
                TargetName = targetName,
                NestedMappings = finalMappings.Values.ToList()
            };
        }

        /// <summary>
        /// Creates a mapping for working with DateTime.Now
        /// </summary>
        public static FunctionMapping MapToUtcNow<TSource>(this string targetName)
        {
            return targetName.MapUsing<TSource, DateTime>(source => DateTime.UtcNow);
        }

        /// <summary>
        /// Creates a mapping for working with Guid.NewGuid
        /// </summary>
        public static FunctionMapping MapToNewGuid<TSource>(this string targetName)
        {
            return targetName.MapUsing<TSource, Guid>(source => Guid.NewGuid());
        }

        /// <summary>
        /// Creates a mapping with a function for dynamically created types
        /// </summary>
        public static FunctionMapping MapUsing(this string targetName, LambdaExpression func)
        {
            return new FunctionMapping
            {
                TargetName = targetName,
                Function = func
            };
        }



        /// <summary>
        /// Creates a multi-source mapping for dynamically created types
        /// </summary>
        public static MultiSourceMapping MapMultiple(this string targetName, LambdaExpression combineFunction, params string[] sourceNames)
        {
            return new MultiSourceMapping
            {
                TargetName = targetName,
                SourceNames = sourceNames.ToList(),
                CombineFunction = combineFunction
            };
        }

        /// <summary>
        /// Creates a mapping for working with DateTime.Now (non-generic version)
        /// </summary>
        public static FunctionMapping MapToUtcNow(this string targetName, Type sourceType)
        {
            // Vytvoříme parametr pro zdrojový typ
            var sourceParam = Expression.Parameter(sourceType, "source");

            // Vytvoříme lambda výraz pro získání UTC času
            var defaultItem = Expression.Variable(typeof(DateTime), "defaultItem");

            var defaultProperty = typeof(DateTime).GetProperty("UtcNow", BindingFlags.Public | BindingFlags.Static)!;

            var lambda = Expression.Lambda(Expression.Property(null, defaultProperty), sourceParam);

            return new FunctionMapping
            {
                TargetName = targetName,
                Function = lambda
            };
        }

        /// <summary>
        /// Creates a mapping for working with Guid.NewGuid (non-generic version)
        /// </summary>
        public static FunctionMapping MapToNewGuid(this string targetName, Type sourceType)
        {
            // Vytvoříme parametr pro zdrojový typ
            var sourceParam = Expression.Parameter(sourceType, "source");

            // Vytvoříme lambda výraz pro generování nového GUID
            var newGuidMethod = typeof(Guid).GetMethod("NewGuid", BindingFlags.Public | BindingFlags.Static);
            if (newGuidMethod == null)
                throw new InvalidOperationException("Guid.NewGuid method not found");

            var newGuidExpression = Expression.Call(newGuidMethod);
            var lambda = Expression.Lambda(newGuidExpression, sourceParam);

            return new FunctionMapping
            {
                TargetName = targetName,
                Function = lambda
            };
        }

        ///// <summary>
        ///// Helper for creating lambda expressions for non-generic mapping
        ///// </summary>
        //public static LambdaExpression CreateMapperExpression(Type sourceType, Type resultType, Expression<Func<ParameterExpression, Expression>> expressionBuilder)
        //{
        //    var sourceParam = Expression.Parameter(sourceType, "source");
        //    if (expressionBuilder is null)
        //        return Expression.Lambda(Expression.Default(resultType), sourceParam);
        //    else
        //    {
        //        var body = expressionBuilder(sourceParam);
        //        return Expression.Lambda(body, sourceParam);
        //    }
        //}
    }

}
