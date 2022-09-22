using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RegisterAttribute : Attribute
    {
        public Type ImplementType { get; private set; }
        public Type InterfaceType { get; private set; }
        public ServiceLifetime Lifetime { get; private set; }
        public RegisterAttribute(Type implementType, ServiceLifetime lifetime, Type interfaceType = null)
        {
            if(implementType.IsAbstract)
            {
                throw new ArgumentException();
            }

            ImplementType = implementType;
            InterfaceType = interfaceType;
            Lifetime = lifetime;
        }
    }
}
