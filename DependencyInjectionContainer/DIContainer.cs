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


        public object Resolve(Type typeToResolve)
            //if TypeToResolve is abstract resolves the first object which implements TypeToResolve
        {
            Service serviceToResolve = services.FirstOrDefault(service => isServiceOfGivenType(service, typeToResolve));

            if (serviceToResolve == null)
            {
                throw new NullReferenceException($"Do not have registrated services of type {typeToResolve.FullName}");
            }

            if(serviceToResolve.Implementation != null)
            {
                return serviceToResolve.Implementation;
            }

            var implementation = GetCreatedImplementationForService(serviceToResolve);

            if(serviceToResolve.Lifetime == ServiceLifetime.Singleton)
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

        public TResolveType Resolve<TResolveType>()
        {
            return (TResolveType)Resolve(typeof(TResolveType));
        }

        public IEnumerable<object> ResolveMany(Type type)
        {
            List<object> resolvedServices = new List<object>();

            var servicesToResolve = services.Where(service => isServiceOfGivenType(service,type)).ToArray();

            foreach (var service in servicesToResolve)
            {
                resolvedServices.Add(Resolve(service.ImplementationType));//create resolve for a service?
            }

            return resolvedServices;
        }

        public IEnumerable<TResolveType> ResolveMany<TResolveType>()
        {
            return ResolveMany(typeof(TResolveType)).Select(service => (TResolveType)service);
        }

        private void ThrowExceptionIfServiceWithTypeExists(Type implementationType)
        {
            bool containsServiceWithType = services.Where(service => service.ImplementationType == implementationType).Any();
            if (containsServiceWithType)
            {
                throw new ArgumentException($"Service with type {implementationType.FullName} has been already registered");
            }
        }
        private bool isServiceOfGivenType(Service service, Type type) => service.InterfaceType == type ||
            service.ImplementationType == type;
    }
}
