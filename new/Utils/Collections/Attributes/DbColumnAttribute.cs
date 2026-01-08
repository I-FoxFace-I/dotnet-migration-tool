namespace IvoEngine.Collections.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute : Attribute
{
    public string ColumnName { get; }

    public DbColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}
