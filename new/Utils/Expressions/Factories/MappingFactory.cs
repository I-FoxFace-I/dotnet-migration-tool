using IvoEngine.Expressions.ExpressionUtils;
using System.Linq.Expressions;
using IvoEngine.Expressions.Mapping;
using IvoEngine.Expressions.Helpers;
using System.Reflection;
using System;


namespace IvoEngine.Expressions.Factories;

/// <summary>
/// Factory class for creating mapping functions between different types
/// </summary>
public static class MappingFactory
{
    /// <summary>
    /// Creates a function for mapping from the source type to the target type
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    /// <param name="options">Mapping options</param>
    /// <returns>Mapping function</returns>
    public static Func<TSource, TTarget> CreateMapper<TSource, TTarget>(MappingOptions options)
    {
        var mappings = options.CreateFinalMappings();
        return CreateMapperInternal<TSource, TTarget>(mappings);
    }

    /// <summary>
    /// Creates a function for mapping using a builder
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    /// <param name="configure">Action for configuring the builder</param>
    /// <returns>Mapping function</returns>
    public static Func<TSource, TTarget> CreateMapper<TSource, TTarget>(Action<MappingBuilder<TSource, TTarget>> configure)
    {
        var builder = new MappingBuilder<TSource, TTarget>();
        configure(builder);
        var options = builder.Build();
        return CreateMapper<TSource, TTarget>(options);
    }

    /// <summary>
    /// Creates an action for updating an existing object
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    /// <param name="options">Mapping options</param>
    /// <returns>Action for updating the object</returns>
    public static Action<TTarget, TSource> CreateUpdaterOld<TSource, TTarget>(MappingOptions options)
    {
        var mappings = options.CreateFinalMappings();
        return CreateUpdaterInternalOld<TSource, TTarget>(mappings);
    }

    public static Func<TTarget, TSource, TTarget> CreateUpdater<TSource, TTarget>(MappingOptions options)
    {
        var mappings = options.CreateFinalMappings();
        return CreateUpdaterInternal<TSource, TTarget>(mappings);
    }

    /// <summary>
    /// Creates a mapping function based on the mapping dictionary
    /// </summary>
    private static Func<TSource, TTarget> CreateMapperInternal<TSource, TTarget>(IReadOnlyDictionary<string, PropertyMapping> mappings)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Parameter for the source object
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create member bindings
        var memberBindings = new List<MemberBinding>();

        // Create mapping for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty == null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Add the binding to the list
                memberBindings.Add(Expression.Bind(targetProperty, mappingExpression));
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error mapping property {mappingPair.Key}: {ex.Message}");
            }
        }

        // Create an instance with initialized values
        var memberInit = Expression.MemberInit(Expression.New(targetType), memberBindings);

        // Create the lambda expression
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(memberInit, sourceParam);

        return lambda.Compile();
    }


    /// <summary>
    /// Creates a mapping function based on the mapping dictionary
    /// </summary>
    private static Func<TSource, TTarget> CreateMapperInternalOld<TSource, TTarget>(IReadOnlyDictionary<string, PropertyMapping> mappings)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Parameter for the source object
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Variable for the target object
        var targetVar = Expression.Variable(targetType, "target");

        var expressions = new List<Expression>();

        // Create a new instance of the target type
        expressions.Add(Expression.Assign(targetVar, Expression.New(targetType)));

        // Create mapping for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty == null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Assign to the target property
                var assignment = Expression.Assign(
                    Expression.Property(targetVar, targetProperty),
                    mappingExpression
                );

                expressions.Add(assignment);
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error mapping property {mappingPair.Key}: {ex.Message}");
            }
        }

        // Return the target object
        expressions.Add(targetVar);

        // Create the lambda expression
        var lambda = Expression.Lambda<Func<TSource, TTarget>>(
            Expression.Block(new[] { targetVar }, expressions),
            sourceParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for updating the object
    /// </summary>
    private static Action<TTarget, TSource> CreateUpdaterInternalOld<TSource, TTarget>(IReadOnlyDictionary<string, PropertyMapping> mappings)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        // Parameters for the target and source object
        var targetParam = Expression.Parameter(targetType, "target");
        var sourceParam = Expression.Parameter(sourceType, "source");

        var expressions = new List<Expression>();

        // Create update for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty == null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Assign to the target property
                var assignment = Expression.Assign(
                    Expression.Property(targetParam, targetProperty),
                    mappingExpression
                );

                expressions.Add(assignment);
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error updating property {mappingPair.Key}: {ex.Message}");
            }
        }

        // Create the lambda expression
        var lambda = Expression.Lambda<Action<TTarget, TSource>>(
            Expression.Block(expressions),
            targetParam,
            sourceParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for updating the object
    /// </summary>
    private static Func<TTarget, TSource, TTarget> CreateUpdaterInternal<TSource, TTarget>(IReadOnlyDictionary<string, PropertyMapping> mappings)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var targetParam = Expression.Parameter(targetType, "target");
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create a variable for the new object
        var outputValue = Expression.Variable(targetType, "newInstance");

        var expressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var isNull = Expression.Equal(sourceParam, Expression.Constant(null, sourceType));

        // If the target type is a record, use the with expression
        if (TypeHelper.IsRecord(targetType) && targetType.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is MethodInfo cloneMethod)
        {
            // Create a copy of the object using the <Clone>$ method
            var cloneCall = Expression.Call(targetParam, cloneMethod);

            // Assign the clone to the new variable
            updateExpressions.Add(Expression.Assign(outputValue, cloneCall));

        }
        else
        {
            // Assign target parameter to variables
            updateExpressions.Add(Expression.Assign(outputValue, targetParam));
        }


        // Create update for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty is null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Assign to the target property
                var assignment = Expression.Assign(
                    Expression.Property(outputValue, targetProperty),
                    mappingExpression
                );

                updateExpressions.Add(assignment);
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error updating property {mappingPair.Key}: {ex.Message}");
            }
        }

        var updateBlock = Expression.Block(updateExpressions);
        // Return the new instance
        expressions.Add(Expression.IfThenElse(
            Expression.Not(isNull),
            updateBlock,
            Expression.Assign(outputValue, targetParam))
        );

        expressions.Add(outputValue);

        // Create the lambda expression
        var lambda = Expression.Lambda<Func<TTarget, TSource, TTarget>>(
            Expression.Block(new[] { outputValue }, expressions),
            targetParam,
            sourceParam
        );

        return lambda.Compile();
    }


    /// <summary>
    /// Creates a bidirectional mapping
    /// </summary>
    /// <typeparam name="TSource">First type</typeparam>
    /// <typeparam name="TTarget">Second type</typeparam>
    /// <param name="forwardOptions">Mapping options from TSource to TTarget</param>
    /// <param name="reverseOptions">Mapping options from TTarget to TSource (optional)</param>
    /// <returns>Bidirectional mapping</returns>
    public static BiDirectionalMapper<TSource, TTarget> CreateBiDirectionalMapper<TSource, TTarget>(
        MappingOptions forwardOptions,
        MappingOptions? reverseOptions = null)
    {
        reverseOptions ??= forwardOptions.CreateReverseMappingOptions();

        var forwardMapper = CreateMapper<TSource, TTarget>(forwardOptions);
        var reverseMapper = CreateMapper<TTarget, TSource>(reverseOptions);
        var forwardUpdater = CreateUpdater<TSource, TTarget>(forwardOptions);
        var reverseUpdater = CreateUpdater<TTarget, TSource>(reverseOptions);

        return new BiDirectionalMapper<TSource, TTarget>(
            forwardMapper,
            reverseMapper,
            forwardUpdater,
            reverseUpdater
        );
    }


    /// <summary>
    /// Creates a function for mapping from the source type to the target type (non-generic version)
    /// </summary>
    /// <param name="sourceType">Source type</param>
    /// <param name="targetType">Target type</param>
    /// <param name="options">Mapping options</param>
    /// <returns>Mapping delegate</returns>
    public static Delegate CreateMapper(Type sourceType, Type targetType, MappingOptions options)
    {
        var mappings = options.CreateFinalMappings();
        return CreateMapperInternal(sourceType, targetType, mappings);
    }

    /// <summary>
    /// Creates a function for mapping using a delegate for configuration
    /// </summary>
    /// <param name="sourceType">Source type</param>
    /// <param name="targetType">Target type</param>
    /// <param name="configure">Delegate for configuring the builder</param>
    /// <returns>Mapping delegate</returns>
    public static Delegate CreateMapper(Type sourceType, Type targetType, Action<NonGenericMappingBuilder> configure)
    {
        var builder = new NonGenericMappingBuilder(sourceType, targetType);
        configure(builder);
        var options = builder.Build();
        return CreateMapper(sourceType, targetType, options);
    }

    /// <summary>
    /// Creates an action for updating an existing object (non-generic version)
    /// </summary>
    /// <param name="sourceType">Source type</param>
    /// <param name="targetType">Target type</param>
    /// <param name="options">Mapping options</param>
    /// <returns>Mapping delegate</returns>
    public static Delegate CreateUpdater(Type sourceType, Type targetType, MappingOptions options)
    {
        var mappings = options.CreateFinalMappings();
        return CreateUpdaterInternal(sourceType, targetType, mappings);
    }

    /// <summary>
    /// Creates a mapping function based on the mapping dictionary (non-generic version)
    /// </summary>
    private static Delegate CreateMapperInternal(Type sourceType, Type targetType, IReadOnlyDictionary<string, PropertyMapping> mappings)
    {

        // Variable for the target object
        var targetVar = Expression.Variable(targetType, "target");

        // Parameter for the source object
        var sourceParam = Expression.Parameter(sourceType, "source");

        var expressions = new List<Expression>();

        // Check if the source object is null
        var isNull = Expression.Equal(sourceParam, Expression.Constant(null, sourceType));

        // Create member bindings
        var memberBindings = new List<MemberBinding>();

        // Create mapping for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty == null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Add the binding to the list
                memberBindings.Add(Expression.Bind(targetProperty, mappingExpression));
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error mapping property {mappingPair.Key}: {ex.Message}");
            }
        }

        // Create an instance with initialized values
        //var memberInit = Expression.MemberInit(Expression.New(targetType), memberBindings);

        // Create the delegate type
        var delegateType = typeof(Func<,>).MakeGenericType(sourceType, targetType);

        var memberInit = Expression.IfThenElse(
                    isNull,
                    Expression.Assign(targetVar, Expression.Constant(null, targetType)),
                    Expression.Assign(targetVar, Expression.MemberInit(Expression.New(targetType), memberBindings))
                );

        expressions.Add(memberInit);
        expressions.Add(targetVar);

        // Create the lambda expression
        var lambda = Expression.Lambda(delegateType, Expression.Block(new[] { targetVar }, expressions), sourceParam);

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for updating the object (non-generic version)
    /// </summary>
    private static Delegate CreateUpdaterInternal(Type sourceType, Type targetType, IReadOnlyDictionary<string, PropertyMapping> mappings)
    {
        // Parameters for the target and source object
        var targetParam = Expression.Parameter(targetType, "target");
        var sourceParam = Expression.Parameter(sourceType, "source");

        // Create a variable for the new object
        var outputValue = Expression.Variable(targetType, "newInstance");

        var expressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        // Check if the source object is null
        var isNull = Expression.Equal(sourceParam, Expression.Constant(null, sourceType));

        // If the target type is a record, use the with expression
        if (TypeHelper.IsRecord(targetType) && targetType.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is MethodInfo cloneMethod)
        {
            // Create a copy of the object using the <Clone>$ method
            var cloneCall = Expression.Call(targetParam, cloneMethod);

            // Assign the clone to the new variable
            updateExpressions.Add(Expression.Assign(outputValue, cloneCall));
        }
        else
        {
            // Assign target parameter to variables
            updateExpressions.Add(Expression.Assign(outputValue, targetParam));
        }

        // Create update for each property
        foreach (var mappingPair in mappings)
        {
            var targetProperty = targetType.GetProperty(mappingPair.Key);
            if (targetProperty is null || !targetProperty.CanWrite)
                continue;

            try
            {
                // Get the mapping expression
                var mappingExpression = mappingPair.Value.GetMappingExpression(sourceParam, sourceType, targetType);

                // Assign to the target property
                var assignment = Expression.Assign(
                    Expression.Property(outputValue, targetProperty),
                    mappingExpression
                );

                updateExpressions.Add(assignment);
            }
            catch (Exception ex)
            {
                // Log the error and continue with other mappings
                Console.WriteLine($"Error updating property {mappingPair.Key}: {ex.Message}");
            }
        }

        var updateBlock = Expression.Block(updateExpressions);
        // Return the new instance
        expressions.Add(Expression.IfThenElse(
            Expression.Not(isNull),
            updateBlock,
            Expression.Assign(outputValue, targetParam))
        );

        expressions.Add(outputValue);

        // Create the delegate type
        var delegateType = typeof(Func<,,>).MakeGenericType(targetType, sourceType, targetType);

        // Create the lambda expression
        var lambda = Expression.Lambda(
            delegateType,
            Expression.Block(new[] { outputValue }, expressions),
            targetParam,
            sourceParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a bidirectional mapping (non-generic version)
    /// </summary>
    /// <param name="sourceType">First type</param>
    /// <param name="targetType">Second type</param>
    /// <param name="forwardOptions">Mapping options from source to target</param>
    /// <param name="reverseOptions">Mapping options from target to source (optional)</param>
    /// <returns>Bidirectional mapping</returns>
    public static object CreateBiDirectionalMapper(Type sourceType, Type targetType, MappingOptions forwardOptions, MappingOptions? reverseOptions = null)
    {
        reverseOptions ??= forwardOptions.CreateReverseMappingOptions();

        var forwardMapper = CreateMapper(sourceType, targetType, forwardOptions);
        var reverseMapper = CreateMapper(targetType, sourceType, reverseOptions);
        var forwardUpdater = CreateUpdater(sourceType, targetType, forwardOptions);
        var reverseUpdater = CreateUpdater(targetType, sourceType, reverseOptions);

        // Create an instance of the generic BiDirectionalMapper<TLeft, TRight> class
        var biDirectionalMapperType = typeof(BiDirectionalMapper<,>).MakeGenericType(sourceType, targetType);
        var constructor = biDirectionalMapperType.GetConstructors().First();

        if (constructor == null)
            throw new InvalidOperationException("BiDirectionalMapper constructor not found");

        return constructor.Invoke(new[] {
                forwardMapper,
                reverseMapper,
                forwardUpdater,
                reverseUpdater
            });
    }
}
