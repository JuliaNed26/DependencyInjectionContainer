namespace DependencyInjectionContainer;
using Attributes;
using Enums;
using Exceptions;
using System.Reflection;

public sealed class DIContainerBuilder
{
    private List<Service> services = new List<Service>();
    private DIContainer? parentContainer;
    private bool isBuild;

    public DIContainerBuilder() { }
    internal DIContainerBuilder(DIContainer parent) => parentContainer = parent;

    public void Register<TImplementationInterface, TImplementation>
        (ServiceLifetime lifetime, Func<DIContainer, TImplementation> implementationFactory = null) where TImplementation : TImplementationInterface
    {
        bool implementationFactoryDefined = implementationFactory != null;
        Func<DIContainer, object> implementationFactoryForService =
            implementationFactoryDefined ? container => implementationFactory(container) : null;

        CheckRegistration(lifetime, typeof(TImplementation), implementationFactoryDefined)
            .services.Add(new Service(typeof(TImplementationInterface), typeof(TImplementation), lifetime, implementationFactoryForService));
    }

    public void Register<TImplementation>
        (ServiceLifetime lifetime, Func<DIContainer, TImplementation> implementationFactory = null) where TImplementation : class
    {
        if (typeof(TImplementation).IsAbstract)
        {
            throw new RegistrationServiceException("Can't register type without assigned implementation type");
        }

        bool implementationFactoryDefined = implementationFactory != null;
        Func<DIContainer, object> implementationFactoryForService =
            implementationFactoryDefined ? container => implementationFactory(container) : null;

        CheckRegistration(lifetime, typeof(TImplementation), implementationFactoryDefined)
            .services.Add(new Service(typeof(TImplementation), lifetime, implementationFactoryForService));
    }

    public void RegisterWithImplementation(object implementation, ServiceLifetime lifetime)
    {
        CheckRegistration(lifetime, implementation.GetType(), true)
            .services.Add(new Service(implementation, lifetime));
    }

    public void RegisterAssemblyByAttributes(Assembly assembly)
    {
        ThrowIfContainerBuilt();

        var typesWithRegisterAttribute = assembly
                                         .GetTypes()
                                         .Where(t => t.GetCustomAttribute<RegisterAttribute>() != null);

        foreach (var type in typesWithRegisterAttribute)
        {
            var serviceInfo = type.GetCustomAttribute<RegisterAttribute>();

                ThrowIfTransientDisposable(serviceInfo.Lifetime, type)
                .ThrowIfImplementationTypeUnappropriate(type)
                .services.Add(serviceInfo.IsRegisteredByInterface
                          ? new Service(type, serviceInfo.Lifetime)
                          : new Service(serviceInfo.InterfaceType, type, serviceInfo.Lifetime));
        }
    }

    public DIContainer Build()
    {
        if (isBuild)
        {
            throw new InvalidOperationException("Container was built already");
        }
        isBuild = true;
        return new DIContainer(services, parentContainer);
    }

    private DIContainerBuilder CheckRegistration(ServiceLifetime lifetime, Type implementationType, bool implementationFactoryDefined)
    {
        return ThrowIfContainerBuilt()
               .ThrowIfManyCtors(implementationType, implementationFactoryDefined)
               .ThrowIfTransientDisposable(lifetime, implementationType)
               .ThrowIfImplementationTypeUnappropriate(implementationType);
    }

    private DIContainerBuilder ThrowIfContainerBuilt()
    {
        if (isBuild)
        {
            throw new RegistrationServiceException("This container was built already");
        }
        return this;
    }

    private DIContainerBuilder ThrowIfTransientDisposable(ServiceLifetime lifetime, Type implementationType)
    {
        if(lifetime == ServiceLifetime.Transient && implementationType.GetInterface("IDisposable") != null)
        {
            throw new RegistrationServiceException("It is prohibited to register transient disposable service");
        }
        return this;
    }

    private DIContainerBuilder ThrowIfImplementationTypeUnappropriate(Type implementationType)
    {
        if (services.Any(service => service.Value == implementationType))
        {
            throw new RegistrationServiceException($"Service with type {implementationType.FullName} has been already registered");
        }
        return this;
    }

    private DIContainerBuilder ThrowIfManyCtors(Type implementationType, bool implementationFactoryDefined)
    {
        if (!implementationFactoryDefined && implementationType.GetConstructors().Count() != 1)
        {
            throw new RegistrationServiceException(
                "It is prohibited to register types with many ctors. Try to define ctor or select type with one ctor");
        }

        return this;
    }
}
