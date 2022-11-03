using DependencyInjectionContainer.Enums;

namespace DependencyInjectionContainer
{
    internal sealed class Service
    {
        internal Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            RegistrationType = RegistrationType.ByInterface;
        }

        internal Service(Type implementationType, ServiceLifetime lifetime)
        {
            ImplementationType = implementationType;
            Lifetime = lifetime;
            RegistrationType = RegistrationType.ByImplementationType;
        }

        internal Service(object implementation, ServiceLifetime lifetime)
        {
            Implementation = implementation;
            Lifetime = lifetime;
            ImplementationType = Implementation.GetType();
            RegistrationType = RegistrationType.ByImplementationType;
        }

        internal RegistrationType RegistrationType { get; init; }
        internal Type InterfaceType { get; init; }
        internal Type ImplementationType { get; init; }
        internal ServiceLifetime Lifetime { get; init; }
        internal object Implementation { get; set; }

        internal bool ImplementsType(Type type)
        {
            bool implementsInterface = type.IsAbstract && RegistrationType == RegistrationType.ByInterface 
                                       && InterfaceType == type;

            return implementsInterface || ImplementationType == type;
        }

    }
}