using System.Reflection;

namespace DependencyInjectionContainer;
using Enums;

internal sealed class Service
{
    internal Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime, Func<DIContainer, object> implementationFactory = null)
    {
        if (!interfaceType.IsAbstract)
            throw new ArgumentException("First type should be abstract");

        Key = interfaceType;
        Value = implementationType;
        Lifetime = lifetime;
        ImplementationFactory = implementationFactory;
    }

    internal Service(Type implementationType, ServiceLifetime lifetime, Func<DIContainer, object> implementationFactory = null)
    {
        Key = Value = implementationType;
        Lifetime = lifetime;
        ImplementationFactory = implementationFactory;
    }

    internal Service(object implementation, ServiceLifetime lifetime)
    {
        Key = Value = implementation.GetType();
        Instance = implementation;
        Lifetime = lifetime;
    }

    internal Type Key { get; init; }
    internal Type Value { get; init; }
    internal ServiceLifetime Lifetime { get; init; }
    internal object Instance { get; set; }
    internal Func<DIContainer, object> ImplementationFactory { get; init; }

    internal bool InstanceCreated() => Instance != null;
    internal bool ImplementationFactoryDefined() => ImplementationFactory != null;
}