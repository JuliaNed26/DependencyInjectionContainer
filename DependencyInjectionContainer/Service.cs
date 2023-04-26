using System.Reflection;
using DependencyInjectionContainer.Exceptions;

namespace DependencyInjectionContainer;
using Enums;

internal sealed class Service
{
    private object? serviceInstance;
    private Func<DiContainer, object>? implementationFactory;

    public Service(Type serviceType, Type implementationType, LifetimeOfService lifetime, Func<DiContainer, object> implementationFactory)
    {
        Key = serviceType;
        Value = implementationType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }
    public Service(Type serviceType, Type implementationType, LifetimeOfService lifetime)
    {
        Key = serviceType;
        Value = implementationType;
        Lifetime = lifetime;
    }

    public Service(Type serviceType, LifetimeOfService lifetime, Func<DiContainer, object> implementationFactory)
    {
        Key = serviceType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }

    public Service(Type serviceType, LifetimeOfService lifetime)
    {
        if (serviceType.IsAbstract)
        {
            throw new ArgumentException("Can't register type without assigned implementation type or factory");
        }
        Key = Value = serviceType;
        Lifetime = lifetime;
    }
    public Service(Type interfaceType, object instance, LifetimeOfService lifetime)
    {
        Key = interfaceType;
        Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    public Service(object instance, LifetimeOfService lifetime)
    {
        Key = Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    public Type Key { get; init; }
    public Type? Value { get; private set; }
    public LifetimeOfService Lifetime { get; private set; }
    
    public object GetOrCreateImplementation_SaveIfSingleton(DiContainer container, ResolveStrategy resolveSource)
    {
        if (serviceInstance is not null)
        {
            return serviceInstance;
        }

        var implementation = GetCreatedImplementationForService();

        if (Lifetime == LifetimeOfService.Singleton)
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

        object GetCreatedImplementationForService()
        {
            if (implementationFactory is not null)
            {
                return implementationFactory(container);
            }

            var ctor = GetAppropriateConstructor();

            var parameters = ctor
                .GetParameters()
                .Select(parameter => container.Resolve(parameter.ParameterType, resolveSource))
                .ToArray();

            var createdImplementation = ctor.Invoke(parameters);
            return createdImplementation;

            ConstructorInfo GetAppropriateConstructor()
            {
                var constructors = Value!.GetConstructors(BindingFlags.Public | BindingFlags.Instance).ToList();
                //BindingFlags.Instance - gets non static members

                if (constructors.Count == 1)
                {
                    return constructors.Single();
                }

                return GetAppropriateConstructorAmongMany(constructors);
            }

            ConstructorInfo GetAppropriateConstructorAmongMany(List<ConstructorInfo> constructorsOfType)
            {
                constructorsOfType = constructorsOfType.OrderByDescending(curCtor => curCtor.GetParameters().Length).ToList();

                ConstructorInfo? appropriateCtor = null;

                foreach (var constructor in constructorsOfType)
                {
                    if (appropriateCtor != null &&
                        constructor.GetParameters().Length < appropriateCtor.GetParameters().Length)
                    {
                        break;
                    }

                    bool isCurAppropriate = true;
                    foreach (var parameter in constructor.GetParameters())
                    {
                        bool containsParameter = !parameter.ParameterType.IsEnumerable() &&
                                                 container.IsServiceRegistered(parameter.ParameterType);

                        bool containsGenericParameter = parameter.ParameterType.IsEnumerable() &&
                                                        container.IsServiceRegistered(
                                                            parameter.ParameterType.GetGenericArguments()[0]);

                        if (!containsParameter && !containsGenericParameter)
                        {
                            isCurAppropriate = false;
                            break;
                        }
                    }

                    if (isCurAppropriate)
                    {
                        appropriateCtor = appropriateCtor is null
                            ? constructor
                            : throw new ResolveServiceException("There's ambiguity when discovering constructors");
                    }
                }

                if (appropriateCtor == null)
                {
                    throw new ResolveServiceException(
                        "Could not find appropriate constructor. Maybe you forgot to register some services or " +
                        "constructor contains value parameter.");
                }

                return appropriateCtor;
            }
        }
    }

    internal void CopyService(Service service)
    {
        Value = service.Value;
        Lifetime = service.Lifetime;
        serviceInstance = service.serviceInstance;
        implementationFactory = service.implementationFactory;
    }
}