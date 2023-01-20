using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;

namespace DependencyInjectionContainer
{
    public sealed class DIContainer : IDisposable
    {
        internal readonly IEnumerable<Service> registeredServices;
        ServicesInstanceList servicesInstanceList;

        internal DIContainer(IEnumerable<Service> services, DIContainer parent)
        {
            servicesInstanceList = new ServicesInstanceList();
            registeredServices = services;
            ContainerParent = parent;
            IsDisposed = false;
        }

        public bool IsDisposed { get; private set; }
        internal DIContainer ContainerParent { get; private set; }

        public DIContainerBuilder CreateChildContainer()
        {
            ThrowIfDisposed();
            return new(this);
        }

        public TTypeToResolve Resolve<TTypeToResolve>(ResolveSource resolveType = ResolveSource.Any) where TTypeToResolve : class
        {
            ThrowIfDisposed();
            return (TTypeToResolve)Resolve(typeof(TTypeToResolve), resolveType);
        }

        public IEnumerable<TTypeToResolve> ResolveMany<TTypeToResolve>(ResolveSource resolveSource = ResolveSource.Any) where TTypeToResolve : class
        {
            ThrowIfDisposed();

            switch (resolveSource)
            {
                case ResolveSource.Any:
                    IEnumerable<TTypeToResolve> resolvedLocal = ResolveLocal();
                    return resolvedLocal.Concat(ResolveNonLocalImplementationTypesWhichWereNotResolved(resolvedLocal));

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
                    .Select(service => (TTypeToResolve)GetOrCreateServiceImplementation_SaveIfSingleton(service, ResolveSource.Local));

            IEnumerable<TTypeToResolve> ResolveNonLocal()
                => ContainerParent != null ? ContainerParent.ResolveMany<TTypeToResolve>() : Enumerable.Empty<TTypeToResolve>();

            IEnumerable<TTypeToResolve> ResolveNonLocalImplementationTypesWhichWereNotResolved(IEnumerable<TTypeToResolve> resolvedServices) =>
                ResolveNonLocal().Where(resolved => !resolvedServices.Any(item => item.GetType() == resolved.GetType()));
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                servicesInstanceList.Dispose();
                servicesInstanceList = null;
                ContainerParent = null;
                GC.SuppressFinalize(this);
                IsDisposed = true;
            }
        }

        private void ThrowIfDisposed()
        { 
            if (IsDisposed)
            {
                throw new NullReferenceException("This container was disposed");
            }
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
                    Service foundService;
                    if (TryGetRegistration(typeToResolve, ResolveSource.Any, out foundService))
                    {
                        return GetOrCreateServiceImplementation_SaveIfSingleton(foundService, resolveSource);
                    }
                    throw new ServiceNotFoundException(typeToResolve);

                case ResolveSource.Local:
                    if (TryGetRegistration(typeToResolve, ResolveSource.Local, out foundService))
                    {
                        return GetOrCreateServiceImplementation_SaveIfSingleton(foundService, resolveSource);
                    }
                    throw new ServiceNotFoundException(typeToResolve);

                default:
                    throw new ArgumentException("Wrong resolve source type");
            }
        }

        private bool TryGetRegistration(Type typeForSearch, ResolveSource resolveSource, out Service foundService)
        {
            List<Service> servicesImplementsType = new List<Service>();


            if(resolveSource == ResolveSource.Local)
            {
                servicesImplementsType = registeredServices
                                         .Where(service => service.Key == typeForSearch)
                                         .ToList();
            }

            else if (resolveSource == ResolveSource.Any)
            {
                var curContainer = this;
                while (curContainer != null && servicesImplementsType.Count == 0)
                {
                    servicesImplementsType = curContainer.registeredServices
                                                         .Where(service => service.Key == typeForSearch)
                                                         .ToList();
                    curContainer = curContainer.ContainerParent;
                }
            }

            if (servicesImplementsType.Count > 1)
            {
                throw new ResolveServiceException($"Many services with type {typeForSearch} was registered. Use ResolveMany to resolve them all");
            }

            foundService = servicesImplementsType.FirstOrDefault();
            return foundService != null;
        }

        private object GetOrCreateServiceImplementation_SaveIfSingleton(Service service, ResolveSource resolveSource)
        {
            if (service.Implementation != null)
            {
                servicesInstanceList.Add(service.Implementation);
                return service.Implementation;
            }

            var implementation = GetCreatedImplementationForService();

            if (service.Lifetime == ServiceLifetime.Singleton)
            {
                service.Implementation = implementation;
                servicesInstanceList.Add(service.Implementation); 
            }

            return implementation;

            object GetCreatedImplementationForService()
            {
                var ctor = GetAppropriateCtor();

                var parameters = ctor
                                 .GetParameters()
                                 .Select(parameter => Resolve(parameter.ParameterType, resolveSource))
                                 .ToArray();

                var implementation = ctor.Invoke(parameters);
                return implementation;

                ConstructorInfo GetAppropriateCtor()
                {
                    var constructors = service.Value
                                       .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .OrderByDescending(ctor => ctor.GetParameters().Count());
                    //BindingFlags.Instance - gets non static members

                    ConstructorInfo appropriateCtor = null;

                    foreach (var constructor in constructors)
                    {
                        if (appropriateCtor != null &&
                            constructor.GetParameters().Count() < appropriateCtor.GetParameters().Count())
                        {
                            break;
                        }

                        bool isCurAppropriate = true;
                        foreach(var parameter in constructor.GetParameters())
                        {
                            Service foundService;
                            if (!parameter.ParameterType.IsEnumerable() && !TryGetRegistration(parameter.ParameterType, resolveSource, out foundService))
                            {
                                isCurAppropriate = false;
                                break;
                            }
                        }

                        if(isCurAppropriate)
                        {
                            if (appropriateCtor == null)
                            {
                                appropriateCtor = constructor;
                            }
                            else
                            {
                                throw new ResolveServiceException("There's ambiguity when discovering constructors");
                            }
                        }
                    }
                    if(appropriateCtor == null)
                    {
                        throw new ResolveServiceException("Could not find appropriate constructor. Maybe you forgot to register some services or " +
                            "constructor contains value parameter.");
                    }
                    return appropriateCtor;
                }
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
