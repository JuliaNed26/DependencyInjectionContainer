namespace DependencyInjectionContainer;
using System;
using System.Collections.Generic;

internal static class TypeExtensions
{
    public static bool IsEnumerable(this Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    public static string GetGenericNameWithoutGenericType(this Type type)
    {
        if (type.IsGenericType)
        {
            return type.FullName is not null 
                ? type.FullName!.Substring(0, type.FullName.IndexOf(type.Name, StringComparison.Ordinal) + type.Name.Length)
                : type.Namespace + '.' + type.Name;
        }

        return "";
    }

    public static bool IsAssignableToGenericType(this Type type, Type typeToCheck)
    {
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

        return type.BaseType?.IsAssignableToGenericType(typeToCheck) ?? false;
    }
}
