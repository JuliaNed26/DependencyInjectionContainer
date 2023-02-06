using System.Reflection;

namespace DependencyInjectionContainer;
using Enums;

internal sealed class Service
{
    private object serviceInstance;
    private Func<DIContainer, object> implementationFactory;

    internal Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime, Func<DIContainer, object> implementationFactory)
    {
        if (!interfaceType.IsAbstract)
            throw new ArgumentException("First type should be abstract");

        Key = interfaceType;
        Value = implementationType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }
    internal Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime)
    {
        if (!interfaceType.IsAbstract)
            throw new ArgumentException("First type should be abstract");

        Key = interfaceType;
        Value = implementationType;
        Lifetime = lifetime;
    }

    internal Service(Type implementationType, ServiceLifetime lifetime, Func<DIContainer, object> implementationFactory)
    {
        Key = Value = implementationType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }

    internal Service(Type implementationType, ServiceLifetime lifetime)
    {
        Key = Value = implementationType;
        Lifetime = lifetime;
    }

    internal Service(object instance, ServiceLifetime lifetime)
    {
        Key = Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    internal Type Key { get; init; }
    internal Type Value { get; init; }
    internal ServiceLifetime Lifetime { get; init; }

    internal object GetOrCreateImplementation_SaveIfSingleton(DIContainer container, ResolveStrategy resolveSource)
    {
        bool instanceCreated = serviceInstance != null;
        bool implementationFactoryDefined = implementationFactory != null;
        if (instanceCreated)
        {
            return serviceInstance;
        }

        var implementation = GetCreatedImplementationForService();

        if (Lifetime == ServiceLifetime.Singleton)
        {
            serviceInstance = implementation;
            if (serviceInstance is IDisposable)
            {
                container.ServicesDisposer.Add((IDisposable)serviceInstance);
            }
        }

        return implementation;

        object GetCreatedImplementationForService()
        {
            if (implementationFactoryDefined)
            {
                return implementationFactory(container);
            }

            var ctor = Value
                                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Single();

            var parameters = ctor
                .GetParameters()
                .Select(parameter => container.Resolve(parameter.ParameterType, resolveSource))
                .ToArray();

            var createdImplementation = ctor.Invoke(parameters);
            return createdImplementation;
        }
    }
}