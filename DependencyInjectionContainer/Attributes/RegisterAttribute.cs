using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependencyInjectionContainer.Enums;

namespace DependencyInjectionContainer.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RegisterAttribute : Attribute
    {
        public RegisterAttribute(Type interfaceType, ServiceLifetime lifetime)
        {
            InterfaceType = interfaceType;
            Lifetime = lifetime;
        }

        public Type InterfaceType { get; private set; }
        public ServiceLifetime Lifetime { get; private set; }
    }
}
