using IvoEngine.Expressions.Helpers;
using IvoEngine.Expressions.StaticData;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace IvoEngine.Expressions.ExpressionUtils;

public static class TypeUtils
{
    private const string DefaultProperty = "Default";
    private const string DefaultUriString = "https://example.com";


    public static Expression DefaultCollection(Type interfaceType)
    {
        var genericArguments = interfaceType.GetGenericArguments();
        var genericDefinition = interfaceType.GetGenericTypeDefinition();

        if (genericDefinition == TypesInfo.EnumerableType)
        {
            return Expression.Constant(Array.CreateInstance(genericArguments.First(), 0));
        }
        else if (TypesInfo.CollectionDefaultTypes.TryGetValue(genericDefinition, out var collectionType))
        {
            if (genericArguments.Length <= 1)
            {
                return Expression.Constant(Activator.CreateInstance(collectionType.MakeGenericType(genericArguments.First())));
            }
            else
            {
                return Expression.Constant(Activator.CreateInstance(collectionType.MakeGenericType(genericArguments[0], genericArguments[1])));
            }
        }
        else
        {
            return Expression.Constant(Array.CreateInstance(genericArguments.First(), 0));
        }
    }

    public static Expression DefaultPrimitive(Type primitiveType)
    {
        if(primitiveType == TypesInfo.StringType)
        {
            return Expression.Constant(string.Empty);
        }
        else if (primitiveType == TypesInfo.UriType)
        {
            return Expression.Constant(new Uri(DefaultUriString));
        }
        else
        {
            return Expression.Default(primitiveType);
        }    
    }


    public static Expression DefaultComparer(Type elementType)
    {
        var genericComparer = TypesInfo.EqualityComparerType.MakeGenericType(elementType);
        var defaultProperty = genericComparer.GetProperty(DefaultProperty, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

        return Expression.Property(null, defaultProperty);
    }

    public static Expression UniversalComparer(Type elementType)
    {
        return Expression.New(TypesInfo.UniversalComparerType.MakeGenericType(elementType));
    }

    public static Expression DefaultInterfaceValue(Type interfaceType, out bool isNull)
    {
        var targetTypes = AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(s => s.GetTypes())
                                                 .Where(t => TypeHelper.IsConcreteImplementation(t, interfaceType))
                                                 .ToList();
        
        if (targetTypes.Any())
        {
            Type? targetType;

            var sortedTypes = TypeHelper.SortByInheritance(targetTypes);

            // First try to find a parameterless constructor
            targetType = sortedTypes.FirstOrDefault(t => t.GetConstructor(Type.EmptyTypes) is not null);

            if (targetType is not null)
            {
                isNull = false;

                return Expression.New(targetType.GetConstructor(Type.EmptyTypes)!);
            }

            foreach (var type in sortedTypes)
            {
                foreach (var constructor in type.GetConstructors().OrderBy(x => x.GetParameters().Count()))
                {
                    try
                    {
                        isNull = false;

                        var parameters = constructor.GetParameters();

                        var emptyParameters = parameters.Select(x => DefaultValueUtils.CreateDefaultValue(x.ParameterType));

                        return Expression.New(constructor, emptyParameters);
                    }
                    catch 
                    {
                        isNull = true;
                    }
                }
            }
        }

        isNull = true;

        return Expression.Default(interfaceType);
    }
}
