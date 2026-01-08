using System.Diagnostics.CodeAnalysis;

namespace IvoEngine.Expressions;

public class UniversalComparer<T>() : IEqualityComparer<T>
    where T : class
{

    public bool Equals(T? lhs, T? rhs)
    {
        return EntityManager<T>.Compare(lhs, rhs);
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return 0;
    }
}
