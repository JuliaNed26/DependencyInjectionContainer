namespace DependencyInjectionContainer;

public static class TypeExtensions
{
    public static bool IsEnumerable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    public static string? GetGenericNameWithoutGenericType(this Type type)
    {
        if (type.IsGenericType)
        {
            return type.FullName is not null 
                ? type.FullName!.Substring(0, type.FullName.IndexOf(type.Name, StringComparison.Ordinal) + type.Name.Length)
                : type.Namespace + '.' + type.Name;
        }

        return null;
    }

    public static bool IsAssignableToGenericTypeDefinition(this Type type, Type typeToCheck)
    {
        if (!typeToCheck.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"{typeToCheck.FullName} is not generic type definition");
        }

        foreach (var t in type.GetInterfaces())
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeToCheck)
            {
                return true;
            }
        }

        if (type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeToCheck)
        {
            return true;
        }

        return type.BaseType?.IsAssignableToGenericTypeDefinition(typeToCheck) ?? false;
    }
}
