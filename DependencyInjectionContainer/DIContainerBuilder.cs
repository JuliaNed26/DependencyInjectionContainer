using JetBrains.Annotations;

namespace DependencyInjectionContainer;
using Attributes;
using Enums;
using Exceptions;
using System.Reflection;

public sealed class DiContainerBuilder
{
    private readonly List<Service> services = new();
    private readonly DiContainer? parentContainer;
    private readonly Rules rules = Rules.None;
    private readonly SecondRegistrationAction secondRegistrationRule = SecondRegistrationAction.Throw;
    private bool isBuild;

    public DiContainerBuilder() { }

    public DiContainerBuilder(Rules givenRules = Rules.None, SecondRegistrationAction action = SecondRegistrationAction.Throw)
    {
        rules = givenRules;
        secondRegistrationRule = action;
    }

    internal DiContainerBuilder(DiContainer parent) => parentContainer = parent;

    public void Register<TServiceType, TImplementation> (LifetimeOfService lifetime) where TImplementation : TServiceType 
        => Register(typeof(TServiceType), typeof(TImplementation), lifetime);

    [AssertionMethod]

    public void Register<TServiceType, TImplementation>
        (LifetimeOfService lifetime, Func<DiContainer, TServiceType> implementationFactory) where TImplementation : TServiceType
        => Register(typeof(TServiceType), typeof(TImplementation), lifetime,
            container => implementationFactory(container)!);

    public void Register<TImplementation> (LifetimeOfService lifetime) where TImplementation : class 
        => Register( typeof(TImplementation), lifetime);

    public void Register<TServiceType>
        (LifetimeOfService lifetime, Func<DiContainer, TServiceType> implementationFactory) where TServiceType : class
       => Register(typeof(TServiceType), lifetime, implementationFactory);

    public void RegisterWithImplementation<TServiceType>(object implementation, LifetimeOfService lifetime)
       => RegisterWithImplementation(typeof(TServiceType), implementation, lifetime);

    public void RegisterWithImplementation(object implementation, LifetimeOfService lifetime)
    {
        ThrowIfContainerBuilt().TreatTransientDisposable(lifetime, implementation.GetType());
        RegisterDependingFromActionOnSecondRegistration(new Service(implementation, lifetime));
    }

    public void RegisterAssemblyByAttributes(Assembly assembly)
    {
        ThrowIfContainerBuilt();

        var typesWithRegisterAttribute = assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<RegisterAttribute>() != null);

        foreach (var type in typesWithRegisterAttribute)
        {
            var serviceInfo = type.GetCustomAttribute<RegisterAttribute>()!;

            ThrowIfContainerBuilt().TreatTransientDisposable(serviceInfo.Lifetime, type);
            TreatWithManyConstructors(type, false);
            var serviceToRegister = serviceInfo.IsRegisteredByInterface
                ? new Service(serviceInfo.InterfaceType!, type, serviceInfo.Lifetime)
                : new Service(type, serviceInfo.Lifetime);
            RegisterDependingFromActionOnSecondRegistration(serviceToRegister);
        }
    }

    public DiContainer Build()
    {
        if (isBuild)
        {
            throw new InvalidOperationException("Container was built already");
        }
        isBuild = true;
        return new DiContainer(services, parentContainer);
    }

    private void RegisterWithImplementation(Type serviceType, object implementation, LifetimeOfService lifetime)
    {
        ThrowIfImplTypeNotConvertibleToServiceType(serviceType, implementation.GetType());
        ThrowIfContainerBuilt()
            .TreatTransientDisposable(lifetime, implementation.GetType());
        RegisterDependingFromActionOnSecondRegistration(new Service(serviceType, implementation, lifetime));
    }

    private void Register(Type interfaceType, Type implementationType, LifetimeOfService lifetime)
    {
        ThrowIfImplTypeNotConvertibleToServiceType(interfaceType, implementationType);
        ThrowIfContainerBuilt().TreatTransientDisposable(lifetime, implementationType)
            .TreatWithManyConstructors(implementationType, false);
        RegisterDependingFromActionOnSecondRegistration(new Service(interfaceType, implementationType, lifetime));
    }

    private void Register(Type implementationType, LifetimeOfService lifetime)
    {
        if (implementationType.IsAbstract)
        {
            throw new RegistrationServiceException("Can't register type without assigned implementation type");
        }
        ThrowIfContainerBuilt()
            .TreatTransientDisposable(lifetime, implementationType).TreatWithManyConstructors(implementationType, false);
        RegisterDependingFromActionOnSecondRegistration(new Service(implementationType, lifetime));
    }

    //how to check that factory returns serviceType
    private void Register(Type serviceType, LifetimeOfService lifetime, Func<DiContainer, object> implementationFactory)
    {
        ThrowIfContainerBuilt().TreatWithManyConstructors(serviceType, true);
        RegisterDependingFromActionOnSecondRegistration(new Service(serviceType, lifetime, implementationFactory));
    }

    //how to check that factory returns serviceType
    private void Register(Type interfaceType, Type implementationType, LifetimeOfService lifetime,
        Func<DiContainer, object> implementationFactory)
    {
        ThrowIfImplTypeNotConvertibleToServiceType(interfaceType, implementationType);
        ThrowIfContainerBuilt().TreatTransientDisposable(lifetime, implementationType)
            .TreatWithManyConstructors(implementationType, false);
        RegisterDependingFromActionOnSecondRegistration(new Service(interfaceType, implementationType, lifetime, implementationFactory));
    }

    private void RegisterDependingFromActionOnSecondRegistration(Service service)
    {
        if (services.Any(localService => localService.Value == service.Value && localService.Key == service.Key))
        {
            switch (secondRegistrationRule)
            {
                case SecondRegistrationAction.Throw:
                    throw new RegistrationServiceException(
                        $"Service with type {service.Key.FullName} has been already registered");
                case SecondRegistrationAction.Ignore:
                    break;
                case SecondRegistrationAction.Rewrite:
                    foreach (var curService in services.Where(x => x.Value == service.Value && x.Key == service.Key))
                    {
                        curService.CopyService(service);
                    }
                    break;
                default:
                    throw new ArgumentException("Do not have such action for second registration");

            }
        }
        else
        {
            services.Add(service);
        }

    }

    private DiContainerBuilder ThrowIfContainerBuilt()
    {
        if (isBuild)
        {
            throw new RegistrationServiceException("This container was built already");
        }
        return this;
    }

    [AssertionMethod]
    private DiContainerBuilder TreatTransientDisposable(LifetimeOfService lifetime, Type implementationType)
    {
        if((rules & Rules.DisposeTransientWhenDisposeContainer) == 0 && 
           lifetime == LifetimeOfService.Transient && implementationType.GetInterface(nameof(IDisposable)) != null)
        {
            throw new RegistrationServiceException("It is prohibited to register transient disposable service");
        }
        return this;
    }

    [AssertionMethod]
    private void TreatWithManyConstructors(Type implementationType, bool factoryExists)
    {
        if ((rules & Rules.GetConstructorWithMostRegisteredParameters) == 0 && !factoryExists
                                                                      && implementationType.GetConstructors().Length != 1)
        {
            throw new RegistrationServiceException(
                "It is prohibited to register types with many constructors. Try to define ctor or select type with one ctor");
        }
    }

    [AssertionMethod]
    private void ThrowIfImplTypeNotConvertibleToServiceType(Type serviceType, Type implementationType)
    {
        if ((serviceType.IsGenericTypeDefinition && !implementationType.IsAssignableToGenericType(serviceType))
            && !implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException(
                $@"Given implementation type {implementationType} is not convertible to type {serviceType}");
        }
    }
}
