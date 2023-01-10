using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer
{
    internal static class TypeExtensions
    {
        public static bool IsEnumerable(this Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
}
