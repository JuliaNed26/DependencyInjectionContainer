using DependencyInjectionContainer.Enums;

namespace DependencyInjectionContainer
{
    internal sealed class Service
    {
        internal Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime)
        {
            if (!interfaceType.IsAbstract)
                throw new ArgumentException("First type should be abstract");

            Key = interfaceType;
            Value = implementationType;
            Lifetime = lifetime;
        }

        internal Service(Type implementationType, ServiceLifetime lifetime)
        {
            Key = Value = implementationType;
            Lifetime = lifetime;
        }

        internal Service(object implementation, ServiceLifetime lifetime)
        {
            Key = Value = implementation.GetType();
            Implementation = implementation;
            Lifetime = lifetime;
        }

        internal Type Key { get; init; }
        internal Type Value { get; init; }
        internal ServiceLifetime Lifetime { get; init; }
        internal object Implementation { get; set; }
    }
}