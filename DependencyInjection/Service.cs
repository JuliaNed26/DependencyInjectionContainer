using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjection
{
    public record Service
    {
        public Type InterfaceType { get; init; }
        public Type ImplementationType { get; init; }
        public object Implementation { get; set; }
        public ServiceLifetime Lifetime { get; init; }

        public Service(Type implementationType, ServiceLifetime lifetime, Type interfaceType = null)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public Service(object implementation, ServiceLifetime lifetime)
        {
            Implementation = implementation;
            Lifetime = lifetime;
            ImplementationType = Implementation.GetType();
        }
    }
}
