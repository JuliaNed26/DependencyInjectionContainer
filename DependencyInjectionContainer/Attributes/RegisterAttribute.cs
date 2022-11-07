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

        public Type InterfaceType { get; }
        public ServiceLifetime Lifetime { get; }
    }
}
