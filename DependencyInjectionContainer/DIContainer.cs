using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer
{
    public class DIContainer
    {
        private List<Service> services;

        public DIContainer()
        {
            services = new List<Service>();
        }

        public void Register<TIType, TImplementation>(ServiceLifetime lifetime) where TImplementation : TIType
        {
            ThrowExceptionIfServiceWithTypeExists(typeof(TImplementation));
            services.Add(new Service(typeof(TImplementation), lifetime, typeof(TIType)));
        }

        public void Register<TImplementation>(ServiceLifetime lifetime)
        {
            if(typeof(TImplementation).IsAbstract)
            {
                throw new ArgumentException("Can't register type without assigned implementation type");
            }
            ThrowExceptionIfServiceWithTypeExists(typeof(TImplementation));
            services.Add(new Service(typeof(TImplementation), lifetime));
        }

        public void Register(object implementation, ServiceLifetime lifetime)
        {
            ThrowExceptionIfServiceWithTypeExists(implementation.GetType());
            services.Add(new Service(implementation, lifetime));
        }

        public TResolveType Resolve<TResolveType>()
        {
            return (TResolveType)Resolve(typeof(TResolveType));
        }

        public IEnumerable<TResolveType> ResolveMany<TResolveType>()
        {
            List<TResolveType> resolvedServices = new List<TResolveType>();

            var servicesToResolve = services.Where(service => IsServiceOfGivenType(service, typeof(TResolveType)))
                                            .Select(service =>
                                            {
                                                resolvedServices.Add((TResolveType)Resolve(service.ImplementationType));
                                                return service;
                                            })
                                            .ToArray();

            return resolvedServices;
        }
        private object Resolve(Type typeToResolve)
        //if TypeToResolve is abstract resolves the first object which implements TypeToResolve
        {
            Service serviceToResolve = services.FirstOrDefault(service => IsServiceOfGivenType(service, typeToResolve));

            if (serviceToResolve == null)
            {
                throw new NullReferenceException($"Do not have registrated services of type {typeToResolve.FullName}");
            }

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
                var constructorInfo = service.ImplementationType.GetConstructors().First();

                var parameters = constructorInfo.GetParameters().Select(parameter => Resolve(parameter.ParameterType)).ToArray();

                object implementation = Activator.CreateInstance(service.ImplementationType, parameters);

                return implementation;
            }
        }
        private void ThrowExceptionIfServiceWithTypeExists(Type implementationType)
        {
            bool containsServiceWithType = services.Any(service => service.ImplementationType == implementationType);
            if (containsServiceWithType)
            {
                throw new ArgumentException($"Service with type {implementationType.FullName} has been already registered");
            }
        }
        private bool IsServiceOfGivenType(Service service, Type type) => service.InterfaceType == type ||
            service.ImplementationType == type;

        private sealed record Service
        {
            public Service(Type implementationType, ServiceLifetime lifetime, Type interfaceType = null)
            {
                InterfaceType = interfaceType;
                ImplementationType = implementationType;
                Lifetime = lifetime;
            }

            public Service(object implementation, ServiceLifetime lifetime)
            {
                Implementation = implementation;
                Lifetime = lifetime;
                ImplementationType = Implementation.GetType();
            }
            public Type InterfaceType { get; init; }
            public Type ImplementationType { get; init; }
            public ServiceLifetime Lifetime { get; init; }
            public object Implementation { get; set; }
        }
    }
}
