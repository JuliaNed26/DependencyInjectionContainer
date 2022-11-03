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

        public DIContainerBuilder CreateChildContainer() => new DIContainerBuilder(this);

        public TypeToResolve Resolve<TypeToResolve>(ResolveSource resolveType = ResolveSource.Any) 
                                    where TypeToResolve : class
        {
            if (IsEnumerable(typeof(TypeToResolve)))
            {
                var typeToResolve = typeof(TypeToResolve).GetGenericArguments()[0];
                return (TypeToResolve)InvokeGenericResolveMany(typeToResolve, this, resolveType);
            }

            if (resolveType == ResolveSource.NonLocal)
            {
                return ResolveNonLocal();
            }

            var servicesImplementsType = registeredServices
                                         .Where(service => service.ImplementsType(typeof(TypeToResolve)))
                                         .ToList();

            if (servicesImplementsType.Count() > 1)
            {
                throw new ArgumentException($"Many services with type {typeof(TypeToResolve)} was registered. Use ResolveMany to resolve them all");
            }

            if (servicesImplementsType.Count() == 0 && (resolveType != ResolveSource.Any || ContainerParent == null))
            {
                throw new ServiceNotFoundException(typeof(TypeToResolve));
            }

            if(servicesImplementsType.Count() == 0)
            {
                return (TypeToResolve)InvokeGenericResolve(typeof(TypeToResolve), ContainerParent, ResolveSource.Any);
            }

            return GetImplementation(servicesImplementsType.First());

            bool IsEnumerable(Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            TypeToResolve ResolveNonLocal()
            {
                if (ContainerParent == null)
                    throw new NullReferenceException("This container does not have a parent");

                return (TypeToResolve)InvokeGenericResolve(typeof(TypeToResolve), ContainerParent, ResolveSource.Any);
            }

            TypeToResolve GetImplementation(Service service)
            {
                if (service.Implementation != null)
                {
                    return (TypeToResolve)service.Implementation;
                }

                var implementation = GetCreatedImplementationForService();

                if (service.Lifetime == ServiceLifetime.Singleton)
                {
                    service.Implementation = implementation;
                }

                return (TypeToResolve)implementation;

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

        public IEnumerable<TypeToResolve> ResolveMany<TypeToResolve>(ResolveSource resolveSource = ResolveSource.Any) 
                          where TypeToResolve : class
        {
            List<TypeToResolve> resolvedServices = new List<TypeToResolve>();

            var resolveLocal = () => resolvedServices
                                                            .AddRange(registeredServices
                                                                      .Where(service => service.ImplementsType(typeof(TypeToResolve)))
                                                                      .Select(service => (TypeToResolve)InvokeGenericResolve(service.ImplementationType, this, ResolveSource.Local)));

            var resolveNonLocal = () =>
            {
                if (ContainerParent != null)
                {
                    resolvedServices.AddRange(ContainerParent.ResolveMany<TypeToResolve>(ResolveSource.Any));
                }
            };

            switch (resolveSource)
            {
                case ResolveSource.Any:
                    resolveLocal();
                    resolveNonLocal();
                    break;
                case ResolveSource.Local:
                    resolveLocal();
                    break;
                case ResolveSource.NonLocal:
                    if (ContainerParent == null)
                        throw new NullReferenceException("This container does not have a parent");
                    resolveNonLocal();
                    break;
                default:
                    throw new ArgumentException("Wrong resolve source");
            }

            return resolvedServices;
        }

        internal bool IsServiceRegistered(Type type) => registeredServices
                                                        .Where(service => service.ImplementsType(type))
                                                        .Any();

        private object InvokeGenericResolveMany(Type typeToResolve, object invokeFrom, ResolveSource resolveSource)
        {
            try
            {
                return typeof(DIContainer) 
                       .GetMethod("ResolveMany")
                       .MakeGenericMethod(new[] { typeToResolve })
                       .Invoke(invokeFrom, new object[] { resolveSource });
            }
            catch(TargetInvocationException ex)
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
                       .GetMethod("Resolve")
                       .MakeGenericMethod(new[] { typeToResolve })
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
