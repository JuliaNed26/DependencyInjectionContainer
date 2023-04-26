namespace DependencyInjectionContainer;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Enums;
using Exceptions;

public sealed class DiContainer : IDisposable, IServiceProvider
{
    private bool isDisposed;
    private readonly IEnumerable<Service> registeredServices;
    private readonly DiContainer? parent;

    public DiContainer()
    {
        ServicesDisposer = new ServicesDisposer();
    }

    internal DiContainer(IEnumerable<Service> services, DiContainer? parent)
    {
        ServicesDisposer = new ServicesDisposer();
        registeredServices = services;
        this.parent = parent;
    }

    public DiContainerBuilder CreateChildContainer()
    {
        ThrowIfDisposed();
        return new DiContainerBuilder(this);
    }

    internal ServicesDisposer ServicesDisposer { get; }

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
                IEnumerable<TTypeToResolve> resolvedLocal = ResolveLocal().ToList(); 
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
                .Select(service => (TTypeToResolve)service.GetOrCreateImplementation_SaveIfSingleton(this, ResolveStrategy.Local));

        IEnumerable<TTypeToResolve> ResolveNonLocal()
            => parent?.ResolveMany<TTypeToResolve>() ?? Enumerable.Empty<TTypeToResolve>();

        IEnumerable<TTypeToResolve> ResolveNonLocalImplementationTypesWhichWereNotResolved(IEnumerable<TTypeToResolve> resolvedServices) =>
            ResolveNonLocal().Where(resolved => resolvedServices.All(item => item.GetType() != resolved.GetType()));
    }

    public bool IsServiceRegistered(Type serviceType) => registeredServices
        .Any(service => service.Key == serviceType || 
                        (serviceType.IsGenericType && service.Key.IsGenericTypeDefinition && service.Key.GetGenericTypeDefinition() == serviceType.GetGenericTypeDefinition()));

    public void Dispose()
    {
        ThrowIfDisposed();
        isDisposed = true;
        ServicesDisposer.Dispose();
    }

    public object GetService(Type serviceType)
    {
        return Resolve(serviceType, ResolveStrategy.Any);
    }

    internal object Resolve(Type typeToResolve, ResolveStrategy resolveSource)
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
                return parent!.Resolve(typeToResolve, ResolveStrategy.Any);

            case ResolveStrategy.Any:
            case ResolveStrategy.Local:
                if (TryGetRegistration(typeToResolve, resolveSource, out Service? foundService))
                {
                    if (typeToResolve.IsGenericType && (foundService!.Value is not null && foundService.Value.IsGenericTypeDefinition))
                    {
                        var genericTypes = typeToResolve.GenericTypeArguments;
                        foundService = new Service(foundService.Key,
                            foundService.Value!.MakeGenericType(genericTypes), foundService.Lifetime);
                    }
                    return foundService!.GetOrCreateImplementation_SaveIfSingleton(this, resolveSource);
                }
                throw new ServiceNotFoundException(typeToResolve);

            default:
                throw new ArgumentException("Wrong resolve source type");
        }
    }

    internal bool TryGetRegistration(Type typeForSearch, ResolveStrategy resolveSource, out Service? foundService)
    {
        List<Service> servicesImplementsType = new ();

        switch (resolveSource)
        {
            case ResolveStrategy.Local:
                servicesImplementsType = registeredServices
                    .Where(service => service.Key == typeForSearch || service.Key.FullName == typeForSearch.GetGenericNameWithoutGenericType())
                    .ToList();

                break;
            case ResolveStrategy.Any:
                servicesImplementsType = TakeFirstRegistered();
                break;
            case ResolveStrategy.NonLocal:
                break;
            default:
                throw new ArgumentException("Wrong resolve source type");
        }
        //check rule
        if (servicesImplementsType.Count > 1)
        {
            throw new ResolveServiceException($"Many services with type {typeForSearch} was registered.");
        }
        
        foundService = servicesImplementsType.SingleOrDefault();
        return foundService != null;

        List<Service> TakeFirstRegistered()
        {
            var curContainer = this;
            List<Service> found = new List<Service>();
            while (curContainer != null && found.Count == 0)
            {
                found = curContainer.registeredServices
                    .Where(service => service.Key == typeForSearch || service.Key.FullName == typeForSearch.GetGenericNameWithoutGenericType())
                    .ToList();
                curContainer = curContainer.parent;
            }
            return found;
        }
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new InvalidOperationException("This container was disposed");
        }
    }

    private static object InvokeGenericResolveMany(Type typeToResolve, object invokeFrom, ResolveStrategy resolveSource)
    {
        try
        {
            return typeof(DiContainer)
                   .GetMethod(nameof(ResolveMany))
                   !.MakeGenericMethod(typeToResolve)
                   .Invoke(invokeFrom, new object[] { resolveSource })!;
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            return null;
        }
    }

    private bool IsParentContainerExist() => parent != null;
}
