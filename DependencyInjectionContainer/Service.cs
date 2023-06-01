using System.Reflection;

using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer.Enums;

namespace DependencyInjectionContainer;

internal sealed class Service
{
    private object? serviceInstance;
    internal Func<DiContainer, object>? ImplementationFactory { get; private set; }

    public Service(Type serviceType, Type implementationType, ServiceLifetime lifetime,
        Func<DiContainer, object> implementationFactory)
    {
        Key = serviceType;
        Value = implementationType;
        Lifetime = lifetime;
        this.ImplementationFactory = implementationFactory;
    }

    public Service(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        Key = serviceType;
        Value = implementationType;
        Lifetime = lifetime;
    }

    public Service(Type serviceType, ServiceLifetime lifetime, Func<DiContainer, object> implementationFactory)
    {
        Key = serviceType;
        Lifetime = lifetime;
        this.ImplementationFactory = implementationFactory;
    }

    public Service(Type serviceType, ServiceLifetime lifetime)
    {
        if (serviceType.IsAbstract)
        {
            throw new ArgumentException("Can't register type without assigned implementation type or factory");
        }

        Key = Value = serviceType;
        Lifetime = lifetime;
    }

    public Service(Type interfaceType, object instance, ServiceLifetime lifetime)
    {
        Key = interfaceType;
        Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    public Service(object instance, ServiceLifetime lifetime)
    {
        Key = Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    public Type Key { get; init; }
    public Type? Value { get; private set; }
    public ServiceLifetime Lifetime { get; private set; }

    public object GetOrCreateImplementation(DiContainer container, ResolveStrategy resolveStrategy)
    {
        if (serviceInstance is not null)
        {
            return serviceInstance;
        }

        var implementation =
            ServiceInstanceCreator.GetCreatedImplementationForService(this, container, resolveStrategy);

        if (Lifetime == ServiceLifetime.Singleton)
        {
            serviceInstance = implementation;
        }

        if (implementation is IDisposable disposableService)
        {
            container.ServicesDisposer.Add(disposableService);
        }

        if (implementation is IAsyncDisposable asyncDisposableService)
        {
            container.ServicesDisposer.Add(asyncDisposableService);
        }

        return implementation;
    }

    internal void CopyService(Service service)
    {
        Value = service.Value;
        Lifetime = service.Lifetime;
        serviceInstance = service.serviceInstance;
        ImplementationFactory = service.ImplementationFactory;
    }
}

internal static class ServiceInstanceCreator
{
    public static object GetCreatedImplementationForService(Service service, DiContainer container, ResolveStrategy resolveStrategy)
    {
        if (service.ImplementationFactory is not null)
        {
            return service.ImplementationFactory(container);
        }

        var ctor = GetAppropriateConstructor(service, container);

        var parameters = ctor
            .GetParameters()
        .Select(parameter => container.Resolve(parameter.ParameterType, resolveStrategy))
            .ToArray();

        var createdImplementation = ctor.Invoke(parameters);
        return createdImplementation;
    }

    private static ConstructorInfo GetAppropriateConstructor(Service service, DiContainer container)
    {
        var constructors = service.Value!.GetConstructors(BindingFlags.Public | BindingFlags.Instance).ToList();
        //BindingFlags.Instance - gets non static members

        if (constructors.Count == 1)
        {
            return constructors.Single();
        }

        return GetAppropriateConstructorAmongMany(constructors, container);
    }

    private static ConstructorInfo GetAppropriateConstructorAmongMany(List<ConstructorInfo> constructorsOfType, DiContainer container)
    {
        constructorsOfType = constructorsOfType.OrderByDescending(curCtor => curCtor.GetParameters().Length).ToList();

        ConstructorInfo? appropriateConstructor = null;

        foreach (var constructor in constructorsOfType)
        {
            if (appropriateConstructor != null &&
                constructor.GetParameters().Length < appropriateConstructor.GetParameters().Length)
            {
                break;
            }

            bool currentConstructorAppropriate = true;
            foreach (var parameter in constructor.GetParameters())
            {
                bool containsParameter = !parameter.ParameterType.IsEnumerable() &&
                container.IsServiceRegistered(parameter.ParameterType);

                bool containsGenericParameter = parameter.ParameterType.IsEnumerable() &&
                container.IsServiceRegistered(
                                                    parameter.ParameterType.GetGenericArguments()[0]);

                if (!containsParameter && !containsGenericParameter)
                {
                    currentConstructorAppropriate = false;
                    break;
                }
            }

            if (currentConstructorAppropriate)
            {
                appropriateConstructor = appropriateConstructor is null
                    ? constructor
                    : throw new ResolveServiceException("There's ambiguity when discovering constructors");
            }
        }

        if (appropriateConstructor == null)
        {
            throw new ResolveServiceException(
                "Could not find appropriate constructor. Maybe you forgot to register some services or " +
                "constructor contains value parameter.");
        }

        return appropriateConstructor;
    }
}

