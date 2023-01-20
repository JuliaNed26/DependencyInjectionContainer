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
        public Type InterfaceType { get; private set; }
        public ServiceLifetime Lifetime { get; private set; }
        public RegisterAttribute(ServiceLifetime lifetime, Type interfaceType = null)
        {
            InterfaceType = interfaceType;
            Lifetime = lifetime;
        }
    }
}
