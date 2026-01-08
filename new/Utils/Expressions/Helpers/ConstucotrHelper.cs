using IvoEngine.Expressions.StaticData;
using System.Reflection;

namespace IvoEngine.Expressions.Helpers;

public static class ConstucotrHelper
{
    /// <summary>
    /// Detects whether a data type has a parameterless constructor
    /// </summary>
    /// <param name="type">Inspected data type</param>
    /// <param name="targetConstructor">Output parameterless constructor, if exits</param>
    /// <returns>True, if given constructor exists, otherwise False</returns>
    public static bool HasSimpleConstructor(Type type, out ConstructorInfo? targetConstructor)
    {
        targetConstructor = default;

        if (type.GetConstructor(Type.EmptyTypes) is ConstructorInfo constructorInfo)
        {
            targetConstructor = constructorInfo;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Detects whether the data type has a constructor with one parameter that is of type IEnumerable
    /// </summary>
    /// <param name="type">Inspected data type</param>
    /// <param name="targetConstructor">Output collection constructor, if exits</param>
    /// <returns>True, if given constructor exists, otherwise False</returns>
    public static bool HasCollectionConstructor(Type type, out ConstructorInfo? targetConstructor)
    {
        targetConstructor = default;

        foreach (var contructorInfo in type.GetConstructors())
        {
            var parameters = contructorInfo.GetParameters();

            if (parameters.Length != 1)
            {
                continue;
            }

            var parameter = parameters.First();

            if (parameter.ParameterType.Name == TypesInfo.EnumerableType.Name)
            {
                targetConstructor = contructorInfo;

                return true;
            }
        }

        return false;
    }
}
