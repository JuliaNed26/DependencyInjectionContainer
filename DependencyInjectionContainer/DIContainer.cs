namespace DependencyInjectionContainer;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Enums;
using Exceptions;

public sealed class DIContainer : IDisposable
{
    private ServicesInstanceList servicesInstanceList = new ServicesInstanceList();
    private bool isDisposed;
    private readonly IEnumerable<Service> registeredServices;
    private DIContainer сontainerParent;

    internal DIContainer(IEnumerable<Service> services, DIContainer parent)
    {
        registeredServices = services;
        сontainerParent = parent;
    }

    public DIContainerBuilder CreateChildContainer()
    {
        ThrowIfDisposed();
        return new(this);
    }

    public TTypeToResolve Resolve<TTypeToResolve>(ResolveStrategy resolveType = ResolveStrategy.Any) where TTypeToResolve : class
    {
        return (TTypeToResolve)Resolve(typeof(TTypeToResolve), resolveType);
    }

    public IEnumerable<TTypeToResolve> ResolveMany<TTypeToResolve>(ResolveStrategy resolveSource = ResolveStrategy.Any) where TTypeToResolve : class
    {
        ThrowIfDisposed();

        switch (resolveSource)
        {
            case ResolveStrategy.Any:
                IEnumerable<TTypeToResolve> resolvedLocal = ResolveLocal(); 
                return resolvedLocal
                      .Concat(ResolveNonLocalImplementationTypesWhichWereNotResolved(resolvedLocal))
                      .ToList();

            case ResolveStrategy.Local:
                return ResolveLocal().ToList();

            case ResolveStrategy.NonLocal:
                {
                    if (!IsParentContainerExist())
                    {
                        throw new NullReferenceException("This container does not have a parent");
                    }
                    return ResolveNonLocal().ToList();
                }
            default:
                throw new ArgumentException("Wrong resolve source");
        }

        IEnumerable<TTypeToResolve> ResolveLocal() =>
            registeredServices
                .Where(service => service.Key == typeof(TTypeToResolve))
                .Select(service => (TTypeToResolve)GetOrCreateServiceImplementation_SaveIfSingleton(service, ResolveStrategy.Local));

        IEnumerable<TTypeToResolve> ResolveNonLocal()
            => IsParentContainerExist() ? сontainerParent.ResolveMany<TTypeToResolve>() : Enumerable.Empty<TTypeToResolve>();

        IEnumerable<TTypeToResolve> ResolveNonLocalImplementationTypesWhichWereNotResolved(IEnumerable<TTypeToResolve> resolvedServices) =>
            ResolveNonLocal().Where(resolved => resolvedServices.All(item => item.GetType() != resolved.GetType()));
    }

    public void Dispose()
    {
        ThrowIfDisposed();
        isDisposed = true;
        servicesInstanceList.Dispose();
    }

    private void ThrowIfDisposed()
    { 
        if (isDisposed)
        {
            throw new InvalidOperationException("This container was disposed");
        }
    }

    private object Resolve(Type typeToResolve, ResolveStrategy resolveSource)
    {
        ThrowIfDisposed();

        if (typeToResolve.IsValueType)
        {
            throw new ArgumentException("Can resolve only reference types");
        }

        if (typeToResolve.IsEnumerable())
        {
            typeToResolve = typeToResolve.GetGenericArguments()[0];
            return InvokeGenericResolveMany(typeToResolve, this, resolveSource);
        }

        switch (resolveSource)
        {
            case ResolveStrategy.NonLocal:
                if (!IsParentContainerExist())
                {
                    throw new NullReferenceException("Current container do not have parent");
                }
                return сontainerParent.Resolve(typeToResolve, ResolveStrategy.Any);

            case ResolveStrategy.Any:
                if (TryGetRegistration(typeToResolve, ResolveStrategy.Any, out Service foundService))
                {
                    return GetOrCreateServiceImplementation_SaveIfSingleton(foundService, resolveSource);
                }
                throw new ServiceNotFoundException(typeToResolve);

            case ResolveStrategy.Local:
                if (TryGetRegistration(typeToResolve, ResolveStrategy.Local, out foundService))
                {
                    return GetOrCreateServiceImplementation_SaveIfSingleton(foundService, resolveSource);
                }
                throw new ServiceNotFoundException(typeToResolve);

            default:
                throw new ArgumentException("Wrong resolve source type");
        }
    }

    private bool TryGetRegistration(Type typeForSearch, ResolveStrategy resolveSource, out Service foundService)
    {
        List<Service> servicesImplementsType = new List<Service>();

        switch (resolveSource)
        {
            case ResolveStrategy.Local:
                servicesImplementsType = registeredServices
                                         .Where(service => service.Key == typeForSearch)
                                         .ToList();
                break;
            case ResolveStrategy.Any:
                servicesImplementsType = GetRegistrationIncludingParentContainer();
                break;
            case ResolveStrategy.NonLocal:
                break;
            default:
                throw new ArgumentException("Wrong resolve source type");
        }

        if (servicesImplementsType.Count > 1)
        {
            throw new ResolveServiceException($"Many services with type {typeForSearch} was registered. Use ResolveMany to resolve them all");
        }

        foundService = servicesImplementsType.SingleOrDefault();
        return IsServiceFound();

        List<Service> GetRegistrationIncludingParentContainer()
        {
            var curContainer = this;
            List<Service> found = new List<Service>();
            while (curContainer != null && found.Count == 0)
            {
                found = curContainer.registeredServices
                    .Where(service => service.Key == typeForSearch)
                    .ToList();
                curContainer = curContainer.сontainerParent;
            }
            return found;
        }

        bool IsServiceFound() => servicesImplementsType.Count() == 1;
    }

    private object GetOrCreateServiceImplementation_SaveIfSingleton(Service service, ResolveStrategy resolveSource)
    {
        if (service.InstanceCreated())
        {
            return service.Instance;
        }

        var implementation = GetCreatedImplementationForService();

        if (service.Lifetime == ServiceLifetime.Singleton)
        {
            service.Instance = implementation;
            servicesInstanceList.Add(service.Instance); 
        }

        return implementation;

        object GetCreatedImplementationForService()
        {
            if (service.ImplementationFactoryDefined())
            {
                return service.ImplementationFactory(this);
            }

            var ctor = service.Value
                                            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                            .SingleOrDefault();

            var parameters = ctor
                                     .GetParameters()
                                     .Select(parameter => Resolve(parameter.ParameterType, resolveSource))
                                     .ToArray();

            var createdImplementation = ctor.Invoke(parameters);
            return createdImplementation;
        }
    }

    private static object InvokeGenericResolveMany(Type typeToResolve, object invokeFrom, ResolveStrategy resolveSource)
    {
        try
        {
            return typeof(DIContainer)
                   .GetMethod(nameof(ResolveMany))
                   .MakeGenericMethod(typeToResolve)
                   .Invoke(invokeFrom, new object[] { resolveSource });
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            return null;
        }
    }

    private bool IsParentContainerExist() => сontainerParent != null;
}
