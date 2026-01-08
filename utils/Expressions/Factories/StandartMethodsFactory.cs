using Utils.Expressions.Factories;
using Utils.Expressions.Helpers;
using Utils.Expressions.ExpressionUtils;
using Utils.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;

namespace Utils.Expressions.Factories;


/// <summary>
/// Provides standard methods for working with entities in EntityManagerNew.
/// </summary>
public static class StandartMethodsFactory
{
    /// <summary>
    /// Creates a function for creating a new instance of the given data type. The instance is created as EMPTY,
    /// i.e., properties not defined with the required keyword are ignored.
    /// If there are any required properties, they are initialized with default or EMPTY values.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for creating a new instance</returns>
    public static Func<T> CreateDefaultFunction<T>(Type type)
    {
        if (type.IsValueType)
        {
            return new Func<T>(() => default);
        }
        else if (type == typeof(string))
        {
            var defaultItem = Expression.Variable(type, "defaultItem");

            return Expression.Lambda<Func<T>>(Expression.Block(new[] { defaultItem }, Expression.Constant(string.Empty))).Compile();
        }
        else
        {
            // Seznam expressions, které slouží k přiřazení required properties v rámci vytváření nového EMPTY objektu
            var expressions = new List<Expression>();

            // Expression pro vytvoření nového objektu, do kterého se následně budou injektovat values pro required properties
            var defaultItem = Expression.Variable(type, "defaultItem");

            // Chceme všechny vlastnosti, které jsou definovány pomocí required keyword. Získáme je pomocí reflexe
            var requiredProperties = type.GetProperties()
                                         .Where(p => p.CustomAttributes.Any(ca => ca.AttributeType == typeof(RequiredMemberAttribute)));

            if (requiredProperties != null && requiredProperties.Any())
            {
                // Pokud daný datový typ obsahuje nějaké required properties, je potřeba provést jejich injekci v rámci vytváření objektu
                var constructor = Expression.New(type);

                // Seznam bindings, které injektují required properties do konstruktoru
                var requiredMemberBindings = new List<MemberBinding>();

                foreach (var requiredProperty in requiredProperties)
                {
                    // Pro danou property získej její datový typ a následně pomocí expression nastav EMPTY hodnotu
                    var propertyType = requiredProperty.PropertyType;

                    // Pokud je propertyType nullable, získáme underlying typ
                    if (TypeHelper.IsNullable(propertyType, out var uPropType) && uPropType is Type underlyingPropertyType)
                    {
                        propertyType = underlyingPropertyType;
                    }

                    // Nastavení EMPTY hodnoty pomocí funkce PropertyDefaultValue
                    requiredMemberBindings.Add(Expression.Bind(requiredProperty, DefaultValueUtils.DefaultValue(propertyType)));
                }

                // Injekce required properties do konstruktoru
                var emptyItemInitialization = Expression.MemberInit(constructor, requiredMemberBindings);

                // Přiřazení hodnoty výstupu pomocí konstruktoru s injektovanými required properties nastavenými na EMPTY value 
                var defaultItemInitialization = Expression.Assign(defaultItem, emptyItemInitialization);

                // Vytvoření výsledné funkce pomocí kompilace vytvořeného expression tree
                return Expression.Lambda<Func<T>>(Expression.Block(new[] { defaultItem }, defaultItemInitialization)).Compile();
            }
            else
            {
                // Pokud typ nemá required vlastnosti, stačí vytvořit instanci pomocí bezparametrického konstruktoru
                if (type.GetConstructor(Type.EmptyTypes) is ConstructorInfo ctor)
                {
                    if (type.IsGenericType)
                    {
                        //ctor = type.MakeGenericType(type.GetGenericArguments()).GetConstructor(Type.EmptyTypes)!;
                        expressions.Add(Expression.Assign(defaultItem, Expression.New(ctor)));
                    }
                    else
                    {
                        expressions.Add(Expression.Assign(defaultItem, Expression.New(ctor)));
                    }
                }
                else
                {
                    expressions.Add(Expression.Assign(defaultItem, Expression.New(type)));
                }

                return Expression.Lambda<Func<T>>(Expression.Block(new[] { defaultItem }, expressions)).Compile();
            }
        }
    }

    /// <summary>
    /// Creates a function for cloning instances of the given data type.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for cloning instances</returns>
    public static Func<T, T> CreateCloneFunctionOld<T>(Type type)
    {
        // Nejprve kontrolujeme, zda typ implementuje vlastní metodu Clone
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == "Clone").FirstOrDefault() is MethodInfo methodInfo)
        {
            // Pokud ano, použijeme tuto metodu
            return new Func<T, T>(x => x is ICloneable origin ? (T)origin.Clone() : x);
        }

        // Pokud ne, vytvoříme vlastní implementaci klonování
        var original = Expression.Parameter(type, "original");
        var clone = Expression.Variable(type, "clone");

        var expressions = new List<Expression>();
        // Vytvoření nové instance typu
        expressions.Add(Expression.Assign(clone, Expression.New(type)));

        // Získáme všechny vlastnosti, které nejsou statické
        var properties = type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true));

        // Pro každou vlastnost zkopírujeme hodnotu z originálu do klonu
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var originalProp = Expression.Property(original, prop);
            var cloneProp = Expression.Property(clone, prop);
            expressions.Add(Expression.Assign(cloneProp, originalProp));
        }

        // Vrátíme výsledný klon
        expressions.Add(clone);

        var lambda = Expression.Lambda<Func<T, T>>(Expression.Block(new[] { clone }, expressions), original);
        return lambda.Compile();
    }

    public static Func<T, T> CreateCloneFunction<T>(Type type)
    {
        // Nejprve kontrolujeme, zda typ implementuje vlastní metodu Clone
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == "Clone").FirstOrDefault() is MethodInfo methodInfo)
        {
            // Pokud ano, použijeme tuto metodu
            return new Func<T, T>(x => x is ICloneable origin ? (T)origin.Clone() : x);
        }

        // Pokud ne, vytvoříme vlastní implementaci klonování
        var original = Expression.Parameter(type, "original");

        // Získáme všechny vlastnosti, které nejsou statické
        var properties = type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true));

        // Vytvoření členských bindingů pro inicializaci
        var memberBindings = new List<MemberBinding>();

        // Pro každou vlastnost vytvoříme binding
        foreach (var prop in properties)
        {
            var originalProp = Expression.Property(original, prop);

            if (prop.CanWrite)
            {
                memberBindings.Add(Expression.Bind(prop, originalProp));
            }
        }

        // Vytvoření instance s inicializací všech vlastností
        var memberInit = Expression.MemberInit(Expression.New(type), memberBindings);

        var lambda = Expression.Lambda<Func<T, T>>(memberInit, original);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a function for comparing two instances of the given data type.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Function for comparing instances</returns>
    public static Func<T, T, bool> CreateCompareFunction<T>(Type type)
    {
        // Nejprve kontrolujeme, zda typ implementuje vlastní metodu Equals
        if (type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == "Equals").FirstOrDefault() is MethodInfo methodInfo)
        {
            // Pokud ano, použijeme tuto metodu
            return new Func<T, T, bool>((x, y) => x?.Equals(y) ?? y?.Equals(x) ?? true);
        }

        var lhs = Expression.Parameter(type, "lhsItem");
        var rhs = Expression.Parameter(type, "rhsItem");

        var expressions = new List<Expression>();

        if (type.IsClass)
        {
            // Pro každou vlastnost vytvoříme výraz pro porovnání
            foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? false)))
            {
                var obj1Prop = Expression.Property(lhs, prop);
                var obj2Prop = Expression.Property(rhs, prop);

                // Vytvoříme výraz pro porovnání hodnot vlastností
                var propComparison = PropertyUtils.ComparisonExpression(obj1Prop, obj2Prop);

                expressions.Add(propComparison);
            }
        }

        var result = Expression.Variable(typeof(bool), "result");

        if (expressions.Any())
        {
            // Zkombinujeme všechny porovnání pomocí logického AND
            var combinedResult = expressions.Aggregate((current, next) => Expression.AndAlso(current, next));

            var block = Expression.Block(new[] { result }, Expression.Assign(result, combinedResult), result);

            var lambda = Expression.Lambda<Func<T, T, bool>>(block, lhs, rhs);

            return lambda.Compile();
        }
        else
        {
            // Pokud nemáme žádné vlastnosti k porovnání, použijeme referenční rovnost
            var equalResult = Expression.Equal(lhs, rhs);

            var block = Expression.Block(new[] { result }, Expression.Assign(result, equalResult), result);

            var lambda = Expression.Lambda<Func<T, T, bool>>(block, lhs, rhs);

            return lambda.Compile();
        }
    }

    public static Action<T, T> CreateUpdateAction<T>(Type type)
    {
        var target = Expression.Parameter(type, "targetItem");
        var source = Expression.Parameter(type, "sourceObject");

        var expressions = new List<Expression>();

        var isNull = Expression.Equal(source, Expression.Constant(null, type));

        // Pro každou vlastnost vytvoříme výraz pro aktualizaci
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var targetProp = Expression.Property(target, prop);
            var sourceProp = Expression.Property(source, prop);

            // Určíme výchozí hodnotu pro případ, že zdrojová hodnota je null
            Expression defaultPropertyValue;
            if (prop.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute attribute)
            {
                // Použijeme hodnotu z atributu DefaultValue
                defaultPropertyValue = Expression.Constant(attribute.Value);
            }
            else
            {
                if (prop.GetCustomAttribute<RequiredMemberAttribute>() is RequiredMemberAttribute requiredAttribute)
                {
                    // Pro required vlastnosti použijeme specializovanou funkci pro vytvoření výchozí hodnoty
                    defaultPropertyValue = DefaultValueUtils.DefaultValue(prop.PropertyType);
                }
                else
                {
                    // Pro ostatní vlastnosti použijeme default
                    defaultPropertyValue = Expression.Default(prop.PropertyType);
                }
            }

            // Aktualizuj vlastnost pouze pokud není jen pro čtení
            if (prop.CanWrite)
            {
                expressions.Add(PropertyUtils.UpdateExpression(targetProp, sourceProp, TypeHelper.IsMarkedAsNullable(prop), defaultPropertyValue));
            }
        }

        var lambda = Expression.Lambda<Action<T, T>>(Expression.Block(expressions), target, source);

        return lambda.Compile();
    }

    public static Func<T, T, T> CreateUpdateFunction<T>(Type type)
    {
        var target = Expression.Parameter(type, "originItem");
        var source = Expression.Parameter(type, "updateSource");

        // Vytvoříme proměnnou pro nový objekt
        var output = Expression.Variable(type, "newInstance");

        var expressions = new List<Expression>();
        var updateExpressions = new List<Expression>();

        var isNull = Expression.Equal(source, Expression.Constant(null, type));

        // Pokud je typ record, používáme with expression
        if (TypeHelper.IsRecord(type) && type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is MethodInfo cloneMethod)
        {
            // Vytvoříme kopii objektu pomocí <Clone>$ metody
            var cloneCall = Expression.Call(target, cloneMethod);

            // Přiřazení klonu do nové proměnné
            updateExpressions.Add(Expression.Assign(output, cloneCall));
        }
        else
        {
            // Proměnné přiřadíme target parameter
            updateExpressions.Add(Expression.Assign(output, target));
        }

        // Pro každou vlastnost vytvoříme výraz pro aktualizaci
        foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
        {
            var targetProp = Expression.Property(output, prop);
            var sourceProp = Expression.Property(source, prop);

            // Určíme výchozí hodnotu pro případ, že zdrojová hodnota je null
            Expression defaultPropertyValue;
            if (prop.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute attribute)
            {
                // Použijeme hodnotu z atributu DefaultValue
                defaultPropertyValue = Expression.Constant(attribute.Value);
            }
            else
            {
                if (prop.GetCustomAttribute<RequiredMemberAttribute>() is RequiredMemberAttribute requiredAttribute)
                {
                    // Pro required vlastnosti použijeme specializovanou funkci pro vytvoření výchozí hodnoty
                    defaultPropertyValue = DefaultValueUtils.DefaultValue(prop.PropertyType);
                }
                else
                {
                    // Pro ostatní vlastnosti použijeme default
                    defaultPropertyValue = Expression.Default(prop.PropertyType);
                }
            }

            // Aktualizuj vlastnost pouze pokud není jen pro čtení
            if (prop.CanWrite)
            {
                updateExpressions.Add(PropertyUtils.UpdateExpression(targetProp, sourceProp, TypeHelper.IsMarkedAsNullable(prop), defaultPropertyValue));
            }
        }

        var updateBlock = Expression.Block(updateExpressions);
        // Vrátíme novou instanci
        expressions.Add(Expression.IfThenElse(
            Expression.Not(isNull),
            updateBlock,
            Expression.Assign(output, target))
        );

        expressions.Add(output);


        var lambda = Expression.Lambda<Func<T, T, T>>(Expression.Block(new[] { output }, expressions), target, source);

        return lambda.Compile();
    }

    /// <summary>
    /// Creates a dictionary of functions for comparing individual properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Dictionary of functions for comparing properties</returns>
    public static Dictionary<string, Func<T, T, bool>> CreatePropertyCompareFunctions<T>(Type type)
    {
        // Pro systémové typy, kolekce nebo typy bez vlastností vrátíme prázdný slovník
        if (TypeHelper.IsPrimitiveType(type))
        {
            return new Dictionary<string, Func<T, T, bool>>();
        }
        else if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return new Dictionary<string, Func<T, T, bool>>();
        }
        else if (!type.GetProperties()?.Any() ?? true)
        {
            return new Dictionary<string, Func<T, T, bool>>();
        }

        var lhs = Expression.Parameter(type, "lhsItem");
        var rhs = Expression.Parameter(type, "rhsItem");

        if (type.IsClass)
        {
            // Vytvoříme slovník s funkcemi pro porovnávání jednotlivých vlastností
            var propertyCompareFunctions = new Dictionary<string, Func<T, T, bool>>();
            foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? false)))
            {
                var obj1Prop = Expression.Property(lhs, prop);
                var obj2Prop = Expression.Property(rhs, prop);

                // Vytvoříme výraz pro porovnání hodnot vlastností
                var propComparison = PropertyUtils.ComparisonExpression(obj1Prop, obj2Prop);

                // Zkompilujeme výraz do funkce
                var lambda = Expression.Lambda<Func<T, T, bool>>(propComparison, lhs, rhs).Compile();

                // Přidáme funkci do slovníku pod názvem vlastnosti
                propertyCompareFunctions.Add(prop.Name, lambda);
            }

            return propertyCompareFunctions;
        }

        return new Dictionary<string, Func<T, T, bool>>();
    }

    /// <summary>
    /// Creates a dictionary of functions for updating individual properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <returns>Dictionary of functions for updating properties</returns>
    public static Dictionary<string, Action<T, T>> CreatePropertyUpdateFunctions<T>(Type type)
    {
        // Pro systémové typy, kolekce nebo typy bez vlastností vrátíme prázdný slovník
        if (TypeHelper.IsPrimitiveType(type))
        {
            return new Dictionary<string, Action<T, T>>();
        }
        else if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return new Dictionary<string, Action<T, T>>();
        }
        else if (!type.GetProperties()?.Any() ?? true)
        {
            return new Dictionary<string, Action<T, T>>();
        }

        var origin = Expression.Parameter(type, "originItem");
        var source = Expression.Parameter(type, "updateSource");

        if (type.IsClass)
        {
            // Vytvoříme slovník s funkcemi pro aktualizaci jednotlivých vlastností
            var propertyUpdateFunctions = new Dictionary<string, Action<T, T>>();
            foreach (var prop in type.GetProperties().Where(x => (!x.GetMethod?.IsStatic ?? true) && (!x.SetMethod?.IsStatic ?? true)))
            {
                var targetProp = Expression.Property(origin, prop);
                var sourceProp = Expression.Property(source, prop);

                // Určíme výchozí hodnotu pro případ, že zdrojová hodnota je null
                Expression defaultPropertyValue;
                if (prop.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute attribute)
                {
                    // Použijeme hodnotu z atributu DefaultValue
                    defaultPropertyValue = Expression.Constant(attribute.Value);
                }
                else
                {
                    if (prop.GetCustomAttribute<RequiredMemberAttribute>() is RequiredMemberAttribute requiredAttribute)
                    {
                        // Pro required vlastnosti použijeme specializovanou funkci pro vytvoření výchozí hodnoty
                        defaultPropertyValue = DefaultValueUtils.DefaultValue(prop.PropertyType);
                    }
                    else
                    {
                        // Pro ostatní vlastnosti použijeme default
                        defaultPropertyValue = Expression.Default(prop.PropertyType);
                    }
                }

                // Aktualizuj vlastnost pouze pokud není jen pro čtení
                if (prop.CanWrite)
                {
                    // Vytvoříme výraz pro aktualizaci hodnoty vlastnosti
                    var propUpdate = PropertyUtils.UpdateExpression(targetProp, sourceProp, TypeHelper.IsMarkedAsNullable(prop), defaultPropertyValue);

                    // Zkompilujeme výraz do funkce
                    var lambda = Expression.Lambda<Action<T, T>>(propUpdate, origin, source).Compile();

                    // Přidáme funkci do slovníku pod názvem vlastnosti
                    propertyUpdateFunctions.Add(prop.Name, lambda);
                }
            }

            return propertyUpdateFunctions;
        }

        return new Dictionary<string, Action<T, T>>();
    }

    /// <summary>
    /// Creates a function for obtaining the hash code of an instance of the given data type.
    /// Allows specifying which properties should be included or excluded from the calculation.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="includeProperties">List of property names to include in the calculation (null for all)</param>
    /// <param name="excludeProperties">List of property names to exclude from the calculation</param>
    /// <returns>Function for obtaining the hash code of an instance</returns>
    public static Func<T, int> CreateHashingMethod<T>(IEnumerable<string>? includeProperties = null, IEnumerable<string>? excludeProperties = null)
    {
        // Získání cílového typu (pokud je T nullable, vezme se jeho underlying typ)
        Type type = TypeHelper.GetTargetType<T>();

        // Kontrola, zda typ implementuje vlastní metodu GetHashCode
        // Hledáme veřejnou instanční metodu bez parametrů, která není zděděná
        bool hasCustomGetHashCode = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.Name == "GetHashCode" && m.GetParameters().Length == 0);

        if (hasCustomGetHashCode)
        {
            // Pokud typ implementuje vlastní metodu GetHashCode, použijeme ji
            // Zajistíme ošetření null hodnot (vrací 0 pro null instance)
            return new Func<T, int>(x => x?.GetHashCode() ?? 0);
        }

        // Získání všech veřejných instančních vlastností typu, které lze číst a nejsou statické
        var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !p.GetMethod.IsStatic)
            .ToList();

        // Filtrování vlastností podle parametrů includeProperties a excludeProperties
        // Určí, které vlastnosti budou zahrnuty do výpočtu hash kódu
        var propertiesToHash = FilterProperties(allProperties, includeProperties, excludeProperties);

        // Vytvoření a kompilace Expression pro výpočet hash kódu na základě vybraných vlastností
        return BuildHashCodeFunction<T>(type, propertiesToHash);
    }

    /// <summary>
    /// Filters the list of properties of a type based on criteria for inclusion and exclusion.
    /// </summary>
    /// <param name="allProperties">List of all properties of the type</param>
    /// <param name="includeProperties">List of property names to include</param>
    /// <param name="excludeProperties">List of property names to exclude</param>
    /// <returns>Filtered list of properties that will be used for calculating the hash code</returns>
    private static List<PropertyInfo> FilterProperties(List<PropertyInfo> allProperties,
        IEnumerable<string>? includeProperties, IEnumerable<string>? excludeProperties)
    {
        // Pokud jsou specifikovány vlastnosti k zahrnutí, použijeme pouze tyto vlastnosti
        if (includeProperties != null && includeProperties.Any())
        {
            // Vytvoříme množinu názvů vlastností pro efektivní vyhledávání, ignorujeme velikost písmen
            var includeSet = new HashSet<string>(includeProperties, StringComparer.OrdinalIgnoreCase);
            // Vrátíme pouze vlastnosti, jejichž název je v množině includeSet
            return allProperties.Where(p => includeSet.Contains(p.Name)).ToList();
        }

        // Pokud jsou specifikovány vlastnosti k vyloučení, vyloučíme je ze seznamu všech vlastností
        if (excludeProperties != null && excludeProperties.Any())
        {
            // Vytvoříme množinu názvů vlastností pro efektivní vyhledávání, ignorujeme velikost písmen
            var excludeSet = new HashSet<string>(excludeProperties, StringComparer.OrdinalIgnoreCase);
            // Vrátíme všechny vlastnosti, jejichž název není v množině excludeSet
            return allProperties.Where(p => !excludeSet.Contains(p.Name)).ToList();
        }

        // Pokud nejsou specifikovány žádné filtry, vrátíme všechny vlastnosti
        return allProperties;
    }

    /// <summary>
    /// Creates and compiles an Expression for calculating the hash code of an instance based on the specified properties.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="type">Target type</param>
    /// <param name="properties">List of properties to include in the hash code calculation</param>
    /// <returns>Function for calculating the hash code of an instance</returns>
    private static Func<T, int> BuildHashCodeFunction<T>(Type type, List<PropertyInfo> properties)
    {
        // Vytvoření parametru pro objekt, pro který budeme počítat hash kód
        var objParam = Expression.Parameter(typeof(T), "obj");

        // Vytvoření lokální proměnné pro uchování mezivýsledku hash kódu
        var hashVar = Expression.Variable(typeof(int), "hash");

        // Inicializace hash kódu na počáteční hodnotu 17 (běžná praxe pro implementaci GetHashCode)
        var initHash = Expression.Assign(hashVar, Expression.Constant(17));

        // Seznam výrazů, které budou postupně vyhodnoceny
        var expressions = new List<Expression> { initHash };

        // Pro každou vlastnost vytvoříme výraz pro kombinaci jejího hash kódu s dosavadním výsledkem
        foreach (var prop in properties)
        {
            // Získání hodnoty vlastnosti z objektu
            var propExpr = Expression.Property(objParam, prop);

            // Vytvoření výrazu pro výpočet hash kódu vlastnosti (s ošetřením různých typů a null hodnot)
            var hashExpr = CreatePropertyHashExpression(propExpr, prop.PropertyType);

            // Kombinace hash kódu podle vzorce: hash = hash * 23 + propHash
            // 23 je běžně používaný multiplikátor v implementacích GetHashCode
            var combineHash = Expression.Assign(
                hashVar,
                Expression.Add(
                    Expression.Multiply(hashVar, Expression.Constant(23)),
                    hashExpr
                )
            );

            // Přidání výrazu pro kombinaci hash kódu do seznamu výrazů
            expressions.Add(combineHash);
        }

        // Přidání výrazu pro vrácení výsledného hash kódu
        expressions.Add(hashVar);

        // Sestavení výsledného bloku výrazů s definicí lokálních proměnných
        var body = Expression.Block(new[] { hashVar }, expressions);

        // Vytvoření lambda výrazu, který přijme objekt typu T a vrátí jeho hash kód
        var lambda = Expression.Lambda<Func<T, int>>(body, objParam);

        // Kompilace lambda výrazu do spustitelné funkce
        return lambda.Compile();
    }

    /// <summary>
    /// Creates an expression for calculating the hash code of a property, taking into account its type and nullability.
    /// </summary>
    /// <param name="propExpr">Expression representing the property value</param>
    /// <param name="propType">Data type of the property</param>
    /// <returns>Expression for calculating the hash code of the property</returns>
    private static Expression CreatePropertyHashExpression(Expression propExpr, Type propType)
    {
        // Pro null hodnoty vrátíme 0 jako hash kód
        var nullHash = Expression.Constant(0);

        // Výraz pro výpočet hash kódu nenulové hodnoty (bude definován dále)
        Expression nonNullHash;

        // Zpracování různých typů vlastností
        if (propType.IsValueType && Nullable.GetUnderlyingType(propType) is null)
        {
            // Pro non-nullable hodnotové typy (int, bool, DateTime, atd.) lze použít přímo GetHashCode
            // Tyto typy nemohou být null, takže není potřeba další ošetření
            nonNullHash = Expression.Call(propExpr, propType.GetMethod("GetHashCode")!);
        }
        else if (typeof(string) == propType)
        {
            // Pro stringy použijeme StringComparer.Ordinal pro konzistentní hash kódy
            // To zajistí, že stejné řetězce budou mít stejný hash kód nezávisle na kultuře
            var stringComparerOrdinal = Expression.Property(null, typeof(StringComparer), "Ordinal");
            nonNullHash = Expression.Call(
                stringComparerOrdinal,
                typeof(StringComparer).GetMethod("GetHashCode", new[] { typeof(string) })!,
                propExpr
            );
        }
        else if (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
        {
            // Pro kolekce (pole, seznamy, slovníky, atd.) vytvoříme hash kombinací hash kódů jednotlivých prvků
            // Stringy jsou také IEnumerable, ale zpracováváme je odděleně výše
            nonNullHash = CreateCollectionHashExpression(propExpr, propType);
        }
        else
        {
            // Pro ostatní typy (reference types, nullable value types) použijeme standardní GetHashCode
            // Může jít o vlastní typy, pro které je potřeba rekurzivní výpočet hash kódu
            nonNullHash = Expression.Call(propExpr, typeof(object).GetMethod("GetHashCode")!);
        }

        if (propType.IsValueType)
        {
            return nullHash;
        }
        else
        {
            // Vytvoření výrazu pro kontrolu, zda je hodnota vlastnosti null
            var isNull = Expression.Equal(propExpr, Expression.Constant(null));

            // Vytvoření podmíněného výrazu: isNull ? 0 : nonNullHash
            // Zajišťuje, že null hodnoty budou mít hash kód 0
            return Expression.Condition(isNull, nullHash, nonNullHash);
        }
    }

    /// <summary>
    /// Creates an expression for calculating the hash code of a collection by combining the hash codes of its elements.
    /// </summary>
    /// <param name="collectionExpr">Expression representing the collection</param>
    /// <param name="collectionType">Data type of the collection</param>
    /// <returns>Expression for calculating the hash code of the collection</returns>
    private static Expression CreateCollectionHashExpression(Expression collectionExpr, Type collectionType)
    {
        // Pro kolekce použijeme cyklus přes prvky a kombinaci jejich hash kódů

        // Získání typu prvků kolekce (např. pro List<int> je to int)
        // Pokud nelze určit typ prvků, použijeme object jako výchozí typ
        Type elementType = TypeHelper.GetElementType(collectionType) ?? typeof(object);

        // Převod kolekce na IEnumerable<elementType> pro jednotné zpracování různých typů kolekcí
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
        var castExpr = Expression.Convert(collectionExpr, enumerableType);

        // Získání metody GetEnumerator pro procházení kolekce
        var getEnumeratorMethod = enumerableType.GetMethod("GetEnumerator");
        var enumeratorExpr = Expression.Call(castExpr, getEnumeratorMethod);

        // Vytvoření proměnné pro enumerátor, který budeme používat pro procházení kolekce
        var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
        var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");

        // Inicializace enumerátoru získaného z kolekce
        var initEnumerator = Expression.Assign(enumeratorVar, enumeratorExpr);

        // Proměnná pro ukládání průběžného hash kódu kolekce
        var hashVar = Expression.Variable(typeof(int), "collectionHash");

        // Inicializace hash kódu kolekce na počáteční hodnotu 17
        var initHash = Expression.Assign(hashVar, Expression.Constant(17));

        // Získání metody MoveNext pro posun na další prvek kolekce
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
        var moveNextExpr = Expression.Call(enumeratorVar, moveNextMethod);

        // Získání aktuálního prvku kolekce pomocí vlastnosti Current enumerátoru
        var currentProp = enumeratorType.GetProperty("Current");
        var currentExpr = Expression.Property(enumeratorVar, currentProp);

        // Výpočet hash kódu aktuálního prvku kolekce
        var itemHashExpr = CreatePropertyHashExpression(currentExpr, elementType);

        // Kombinace hash kódu prvku s dosavadním hash kódem kolekce
        // collectionHash = collectionHash * 23 + itemHash
        var combineHash = Expression.Assign(
            hashVar,
            Expression.Add(
                Expression.Multiply(hashVar, Expression.Constant(23)),
                itemHashExpr
            )
        );

        // Tělo cyklu while - kombinace hash kódu pro každý prvek
        var loopBody = Expression.Block(combineHash);

        // Vytvoření cyklu while (enumerator.MoveNext()) { loopBody }
        var whileLoop = Expression.Loop(
            Expression.IfThenElse(
                moveNextExpr,
                loopBody,
                Expression.Break(Expression.Label())
            ),
            Expression.Label()
        );

        // Vytvoření kompletního bloku pro výpočet hash kódu kolekce
        // 1. Inicializace enumerátoru
        // 2. Inicializace hash kódu
        // 3. Procházení prvků a kombinace jejich hash kódů
        // 4. Vrácení výsledného hash kódu
        var blockExpr = Expression.Block(
            new[] { enumeratorVar, hashVar },
            initEnumerator,
            initHash,
            whileLoop,
            hashVar
        );

        return blockExpr;
    }

    /// <summary>
    /// Extension method for easy calculation of the hash code of any object.
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="obj">Object for which to calculate the hash code</param>
    /// <param name="includeProperties">Properties to include in the hash code calculation (null for all)</param>
    /// <param name="excludeProperties">Properties to exclude from the hash code calculation</param>
    /// <returns>Hash code of the object</returns>
    public static int GetHashCode<T>(this T obj, IEnumerable<string>? includeProperties = null, IEnumerable<string>? excludeProperties = null)
    {
        // Delegování výpočtu hash kódu na metodu v EntityManager<T>
        return EntityManager<T>.GetHashCode(obj, includeProperties, excludeProperties);
    }
}
