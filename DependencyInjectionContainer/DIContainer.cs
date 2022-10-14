using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using DependencyInjectionContainer.Attributes;
using System.Collections;
using System.Data;
using System.Reflection.Metadata;

namespace DependencyInjectionContainer
{
    public sealed record DIContainer
    {
        private DIContainer curContainerParent;

        internal DIContainer(List<Service> _services, DIContainer containerParent)
        {
            Services = _services;
            curContainerParent = containerParent;
        }
        internal List<Service> Services { get; private set; }

        public DIContainerBuilder CreateAChildContainer()
        {
            return new DIContainerBuilder(this);
        }

        public TypeToResolve Resolve<TypeToResolve>(ResolveSource resolveType = ResolveSource.Any) where TypeToResolve : class?
        {
            return (TypeToResolve)Resolve(typeof(TypeToResolve), resolveType);
        }

        public IEnumerable<TypeToResolve> ResolveMany<TypeToResolve>(ResolveSource resolveSource = ResolveSource.Any) where TypeToResolve : class?
        {
            return ResolveMany(typeof(TypeToResolve), resolveSource).Select(obj => (TypeToResolve)obj);
        }

        private IEnumerable<object> ResolveMany(Type typeToResolve, ResolveSource resolveSource = ResolveSource.Any)
        {

            List<object> resolvedServices = new List<object>();

            var resolveLocal = (ResolveSource resSource) =>
                    resolvedServices.AddRange(Services.Where(service => IsServiceOfGivenType(service, typeToResolve))
                                            .Select(service => Resolve(service.ImplementationType, resSource)).ToList());

            var resolveNonLocal = (ResolveSource resSource) =>
            { if (curContainerParent != null) { resolvedServices.AddRange(curContainerParent.ResolveMany(typeToResolve, resSource)); } };

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
                    if (curContainerParent == null)
                        throw new ArgumentException("This container does not have a parent");
                    resolveNonLocal(ResolveSource.Any);
                    break;
                default:
                    throw new ArgumentException("Wrong resolve source");
                    break;
            }

            return resolvedServices;
        }

        private object Resolve(Type typeToResolve, ResolveSource resolveType)
        {
            if(typeToResolve.IsGenericType && typeToResolve.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return ResolveMany(typeToResolve.GetGenericArguments()[0], resolveType);
            }

            if(resolveType == ResolveSource.NonLocal)
            {
                if(curContainerParent == null)
                    throw new ArgumentException("This container does not have a parent");

                return curContainerParent.Resolve(typeToResolve, ResolveSource.Any);
            } 

            var servicesToResolve = Services.Where(service => IsServiceOfGivenType(service, typeToResolve));

            if (servicesToResolve.Count() > 1)
            {
                throw new ArgumentException($"Many services with type {typeToResolve} was registered. Use ResolveMany to resolve them all");
            }

            if (servicesToResolve.Count() == 0)
            {
                if (curContainerParent != null && resolveType == ResolveSource.Any)
                {
                    return curContainerParent.Resolve(typeToResolve, resolveType);
                }
                else
                {
                    throw new KeyNotFoundException($"Do not have registrated services of type {typeToResolve.FullName}");
                }
            }

            Service serviceToResolve = servicesToResolve.First();

            if (serviceToResolve.Implementation != null)
            {
                return serviceToResolve.Implementation;
            }

            var implementation = GetCreatedImplementationForService(serviceToResolve);

            if (serviceToResolve.Lifetime == ServiceLifetime.Singleton)
            {
                serviceToResolve.Implementation = implementation;
            }

            return implementation;

            object GetCreatedImplementationForService(Service service)
            {
                var IsEnumerable = (Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                var parameters = service.ImplementationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First()?.
                                 GetParameters()
                                 .Select(parameter => 
                                 { 
                                     if (!IsEnumerable(parameter.ParameterType)) 
                                     {
                                         return Resolve(parameter.ParameterType, resolveType);
                                     }
                                     return ResolveMany(parameter.ParameterType.GetGenericArguments()[0], resolveType);
                                 }).ToArray();

                return Activator.CreateInstance(service.ImplementationType, parameters);
            }
        }

        private bool IsServiceOfGivenType(Service service, Type type) => service.InterfaceType == type ||
            service.ImplementationType == type;
    }
}
