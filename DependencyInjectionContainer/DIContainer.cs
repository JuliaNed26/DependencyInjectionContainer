using System.Reflection;
using System.Runtime.ExceptionServices;
using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;

namespace DependencyInjectionContainer
{
    public sealed class DIContainer
    {
        private readonly IEnumerable<Service> registeredServices;

        internal DIContainer(IEnumerable<Service> services, DIContainer parent)
        {
            registeredServices = services;
            ContainerParent = parent;
        }

        internal DIContainer ContainerParent { get; init; }

        public DIContainerBuilder CreateChildContainer() => new(this);

        public TTypeToResolve Resolve<TTypeToResolve>(ResolveSource resolveType = ResolveSource.Any) where TTypeToResolve : class
        {
            TTypeToResolve resolvedService;

            if (TryResolveIfEnumerable(out resolvedService))
            {
                return resolvedService;
            }

            var serviceToResolve = FindServiceImplementsType();

            switch (resolveType)
            {
                case ResolveSource.NonLocal:
                    resolvedService = ResolveNonLocal();
                    break;

                case ResolveSource.Any:
                    if(serviceToResolve == null && ContainerParent == null)
                    {
                        throw new ServiceNotFoundException(typeof(TTypeToResolve));
                    }
                    resolvedService = serviceToResolve == null
                                      ? ResolveNonLocal() 
                                      : GetImplementation(serviceToResolve);
                    break;

                case ResolveSource.Local:
                    if(serviceToResolve == null)
                    {
                        throw new ServiceNotFoundException(typeof(TTypeToResolve));
                    }
                    resolvedService = GetImplementation(serviceToResolve);
                    break;

            }

            return resolvedService;

            bool TryResolveIfEnumerable(out TTypeToResolve resolved)
            {
                if (IsEnumerable(typeof(TTypeToResolve)))
                {
                    var typeToResolve = typeof(TTypeToResolve).GetGenericArguments()[0];
                    resolved = (TTypeToResolve)InvokeGenericResolveMany(typeToResolve, this, resolveType);
                    return true;
                }
                resolved = null;
                return false;

                bool IsEnumerable(Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }

            Service FindServiceImplementsType()
            {
                var servicesImplementsType = registeredServices
                                             .Where(service => service.ImplementsType(typeof(TTypeToResolve)))
                                             .ToList();


                if (servicesImplementsType.Count > 1)
                {
                    throw new ArgumentException($"Many services with type {typeof(TTypeToResolve)} was registered. Use ResolveMany to resolve them all");
                }

                return servicesImplementsType.Count == 0 ? null : servicesImplementsType.First();
            }

            TTypeToResolve ResolveNonLocal()
            {
                if (ContainerParent == null)
                    throw new NullReferenceException("This container does not have a parent");

                return (TTypeToResolve)InvokeGenericResolve(typeof(TTypeToResolve), ContainerParent, ResolveSource.Any);
            }

            TTypeToResolve GetImplementation(Service service)
            {
                if (service.Implementation != null)
                {
                    return (TTypeToResolve)service.Implementation;
                }

                var implementation = GetCreatedImplementationForService();

                if (service.Lifetime == ServiceLifetime.Singleton)
                {
                    service.Implementation = implementation;
                }

                return (TTypeToResolve)implementation;

                object GetCreatedImplementationForService()
                {
                    var ctor = service.ImplementationType
                                      .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                      .First();
                    //BindingFlags.Instance - gets non static members

                    var parameters = ctor
                                     .GetParameters()
                                     .Select(parameter => InvokeGenericResolve(parameter.ParameterType, this, resolveType))
                                     .ToArray();

                    var implementation = ctor.Invoke(parameters);
                    return implementation;
                }
            }
        }

        public IEnumerable<TTypeToResolve> ResolveMany<TTypeToResolve>(ResolveSource resolveSource = ResolveSource.Any) where TTypeToResolve : class
        {
            switch (resolveSource)
            {
                case ResolveSource.Any:
                    return ResolveLocal().Concat(ResolveNonLocal());
                case ResolveSource.Local:
                    return ResolveLocal();
                case ResolveSource.NonLocal:
                {
                    if (ContainerParent == null)
                    {
                        throw new NullReferenceException("This container does not have a parent");
                    }
                    return ResolveNonLocal();
                }
                default:
                    throw new ArgumentException("Wrong resolve source");
            }

            IEnumerable<TTypeToResolve> ResolveLocal() =>
                registeredServices
                    .Where(service => service.ImplementsType(typeof(TTypeToResolve)))
                    .Select(service => (TTypeToResolve)InvokeGenericResolve(service.ImplementationType, this, ResolveSource.Local));

            IEnumerable<TTypeToResolve> ResolveNonLocal()
            {
                if (ContainerParent != null)
                {
                    return ContainerParent.ResolveMany<TTypeToResolve>();
                }

                return Enumerable.Empty<TTypeToResolve>();
            }
        }

        internal bool IsServiceRegistered(Type type) => registeredServices.Any(service => service.ImplementsType(type));

        private static object InvokeGenericResolveMany(Type typeToResolve, object invokeFrom, ResolveSource resolveSource)
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

        private object InvokeGenericResolve(Type typeToResolve, object invokeFrom, ResolveSource resolveSource)
        {
            try
            {
                return typeof(DIContainer)
                       .GetMethod(nameof(Resolve))
                       .MakeGenericMethod(typeToResolve)
                       .Invoke(invokeFrom, new object[] { resolveSource });
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return null;
            }
        }

    }
}
