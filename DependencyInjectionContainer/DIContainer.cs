using System.Reflection;
using System.Data;
using DependencyInjectionContainer.Exceptions;

namespace DependencyInjectionContainer
{
    public sealed record DIContainer
    {
        private DIContainer containerParent;

        internal DIContainer(List<Service> _services, DIContainer _containerParent)
        {
            Services = _services;
            containerParent = _containerParent;
        }

        internal List<Service> Services { get; private set; }

        public DIContainerBuilder CreateAChildContainer()
        {
            return new DIContainerBuilder(this);
        }

        public TypeToResolve Resolve<TypeToResolve>(ResolveSource resolveType = ResolveSource.Any) where TypeToResolve : class?
        {
            if (IsEnumerable(typeof(TypeToResolve)))
            {
                return (TypeToResolve)InvokeResolveManyForIEnumerable(typeof(TypeToResolve), this, resolveType);
            }

            if (resolveType == ResolveSource.NonLocal)
            {
                return ResolveNonLocal();
            }

            var servicesToResolve = Services.Where(service => IsServiceOfGivenType(service, typeof(TypeToResolve)));

            if (servicesToResolve.Count() > 1)
            {
                throw new ArgumentException($"Many services with type {typeof(TypeToResolve)} was registered. Use ResolveMany to resolve them all");
            }

            if (servicesToResolve.Count() == 0)
            {
                if (containerParent != null && resolveType == ResolveSource.Any)
                {
                    return (TypeToResolve)InvokeGenericResolve(typeof(TypeToResolve), containerParent, ResolveSource.Any);
                }
                throw new ServiceNotFoundException(typeof(TypeToResolve));
            }

            Service serviceToResolve = servicesToResolve.First();

            if (serviceToResolve.Implementation != null)
            {
                return (TypeToResolve)serviceToResolve.Implementation;
            }

            var implementation = GetCreatedImplementationForService(serviceToResolve);

            if (serviceToResolve.Lifetime == ServiceLifetime.Singleton)
            {
                serviceToResolve.Implementation = implementation;
            }

            return (TypeToResolve)implementation;

            TypeToResolve ResolveNonLocal()
            {
                if (containerParent == null)
                    throw new NullReferenceException("This container does not have a parent");

                return (TypeToResolve)InvokeGenericResolve(typeof(TypeToResolve), containerParent, ResolveSource.Any);
            }

            object GetCreatedImplementationForService(Service service)
            {
                var ctor = service.ImplementationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
                //BindingFlags.Instance - gets non static members

                var parameters = ctor?.
                                 GetParameters()
                                 .Select(parameter =>
                                 {
                                     if (IsEnumerable(parameter.ParameterType))
                                     {
                                         return InvokeResolveManyForIEnumerable(parameter.ParameterType, this, resolveType);
                                     }
                                     return InvokeGenericResolve(parameter.ParameterType, this, resolveType);
                                 }).ToArray();

                var implementation = ctor.Invoke(parameters);
                return implementation;
            }

            bool IsEnumerable(Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public IEnumerable<TypeToResolve> ResolveMany<TypeToResolve>(ResolveSource resolveSource = ResolveSource.Any) where TypeToResolve : class?
        {
            List<TypeToResolve> resolvedServices = new List<TypeToResolve>();

            var resolveLocal = (ResolveSource resSource) =>
                    resolvedServices.AddRange(Services.Where(service => IsServiceOfGivenType(service, typeof(TypeToResolve)))
                                            .Select(service => (TypeToResolve)InvokeGenericResolve(service.ImplementationType, this, resSource)));

            var resolveNonLocal = (ResolveSource resSource) =>
            { if (containerParent != null) { resolvedServices.AddRange(containerParent.ResolveMany<TypeToResolve>(resSource)); } };

            switch (resolveSource)
            {
                case ResolveSource.Any:
                    resolveLocal(resolveSource);
                    resolveNonLocal(resolveSource);
                    break;
                case ResolveSource.Local:
                    resolveLocal(resolveSource);
                    break;
                case ResolveSource.NonLocal:
                    if (containerParent == null)
                        throw new NullReferenceException("This container does not have a parent");
                    resolveNonLocal(ResolveSource.Any);
                    break;
                default:
                    throw new ArgumentException("Wrong resolve source");
                    break;
            }

            return resolvedServices;
        }

        private bool IsServiceOfGivenType(Service service, Type type) => service.InterfaceType == type ||
            service.ImplementationType == type;

        private object InvokeResolveManyForIEnumerable(Type typeWithGeneric, object invokeFrom, ResolveSource resolveSource)
        {
            try
            {
                var typeToResolve = typeWithGeneric.GetGenericArguments()[0];
                var resolveManyGeneric = typeof(DIContainer).GetMethod("ResolveMany").MakeGenericMethod(new[] { typeToResolve });
                return resolveManyGeneric.Invoke(invokeFrom, new object[] { resolveSource });
            }
            catch(TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private object InvokeGenericResolve(Type typeToResolve, object invokeFrom, ResolveSource resolveSource)
        {
            try
            {
                var resolveManyGeneric = typeof(DIContainer).GetMethod("Resolve").MakeGenericMethod(new[] { typeToResolve });
                return resolveManyGeneric.Invoke(invokeFrom, new object[] { resolveSource });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
