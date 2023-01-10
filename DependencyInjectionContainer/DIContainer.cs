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
            return (TTypeToResolve)Resolve(typeof(TTypeToResolve), resolveType);
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
                    .Where(service => service.Key == typeof(TTypeToResolve))
                    .Select(service => (TTypeToResolve)GetOrCreateServiceImplementationSaveIfSingleton(service, ResolveSource.Local));

            IEnumerable<TTypeToResolve> ResolveNonLocal()
                => ContainerParent != null ? ContainerParent.ResolveMany<TTypeToResolve>() : Enumerable.Empty<TTypeToResolve>();
        }

        internal bool IsImplementationRegistered(Type type)
        {
            if (type.IsAbstract)
                throw new ArgumentException("Implementation could not be abstract type");

            return registeredServices.Any(service => service.Value == type);
        }

        private object Resolve(Type typeToResolve, ResolveSource resolveSource)
        {
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
                case ResolveSource.NonLocal:
                    if (ContainerParent == null)
                    {
                        throw new NullReferenceException("Current container do not have parent");
                    }

                    return ContainerParent.Resolve(typeToResolve, ResolveSource.Any);

                case ResolveSource.Any:
                    Service foundLocal;
                    if (TryGetRegistration(typeToResolve, out foundLocal))
                    {
                        return GetOrCreateServiceImplementationSaveIfSingleton(foundLocal, resolveSource);
                    }

                    if (ContainerParent == null)
                    {
                        throw new ServiceNotFoundException(typeToResolve);
                    }

                    return ContainerParent.Resolve(typeToResolve, ResolveSource.Any);

                case ResolveSource.Local:
                    if (!TryGetRegistration(typeToResolve, out foundLocal))
                    {
                        throw new ServiceNotFoundException(typeToResolve);
                    }

                    return GetOrCreateServiceImplementationSaveIfSingleton(foundLocal, resolveSource);

                default:
                    throw new ArgumentException("Wrong resolve source type");
            }
        }

        private bool TryGetRegistration(Type typeForSearch, out Service foundService)
        {
            var servicesImplementsType = registeredServices
                                         .Where(service => service.Key == typeForSearch)
                                         .ToList();


            if (servicesImplementsType.Count > 1)
            {
                throw new ArgumentException($"Many services with type {typeForSearch} was registered. Use ResolveMany to resolve them all");
            }

            foundService = servicesImplementsType.FirstOrDefault();
            return foundService != null;
        }

        private object GetOrCreateServiceImplementationSaveIfSingleton(Service service, ResolveSource resolveSource)
        {
            if (service.Implementation != null)
            {
                return service.Implementation;
            }

            var implementation = GetCreatedImplementationForService();

            if (service.Lifetime == ServiceLifetime.Singleton)
            {
                service.Implementation = implementation;
            }

            return implementation;

            object GetCreatedImplementationForService()
            {
                var ctor = service.Value
                                  .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                  .First();
                //BindingFlags.Instance - gets non static members

                var parameters = ctor
                                 .GetParameters()
                                 .Select(parameter => Resolve(parameter.ParameterType, resolveSource))
                                 .ToArray();

                var implementation = ctor.Invoke(parameters);
                return implementation;
            }
        }

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


    }
}
