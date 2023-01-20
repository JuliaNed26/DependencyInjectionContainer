using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public sealed class DIContainerBuilder
    {
        private List<Service> services;
        private DIContainer parentContainer;
        private bool isBuild;

        public DIContainerBuilder()
        {
            services = new List<Service>();
            isBuild = false;
        }
        internal DIContainerBuilder(DIContainer parent) : this() => parentContainer = parent;

        public void Register<TImplementationInterface, TImplementation>(ServiceLifetime lifetime) where TImplementation : TImplementationInterface
        {
            CheckRegistration(lifetime, typeof(TImplementation))
                .services.Add(new Service(typeof(TImplementationInterface), typeof(TImplementation), lifetime));
        }

        public void Register<TImplementation>(ServiceLifetime lifetime) where TImplementation : class
        {
            if (typeof(TImplementation).IsAbstract)
            {
                throw new RegistrationServiceException("Can't register type without assigned implementation type");
            }
            CheckRegistration(lifetime, typeof(TImplementation))
                .services.Add(new Service(typeof(TImplementation), lifetime));
        }

        public void RegisterWithImplementation(object implementation, ServiceLifetime lifetime)
        {
            CheckRegistration(lifetime, implementation.GetType())
                .services.Add(new Service(implementation, lifetime));
        }

        public void RegisterAssemblyByAttributes(Assembly assembly)
        {
            ThrowIfContainerBuilt();

            var typesWithRegisterAttribute = assembly
                                             .GetTypes()
                                             .Where(t => t.GetCustomAttribute<RegisterAttribute>() != null);

            foreach (var type in typesWithRegisterAttribute)
            {
                var serviceInfo = type.GetCustomAttribute<RegisterAttribute>();
                ThrowIfTransientDisposable(serviceInfo.Lifetime, type)
                .ThrowIfImplementationTypeUnappropriate(type)
                .services.Add(serviceInfo.InterfaceType == null 
                              ? new Service(type, serviceInfo.Lifetime)
                              : new Service(serviceInfo.InterfaceType, type, serviceInfo.Lifetime));
            }
        }

        public DIContainer Build()
        {
            if (isBuild)
            {
                throw new InvalidOperationException("Container was built already");
            }
            isBuild = true;
            return new DIContainer(services, parentContainer);
        }

        private DIContainerBuilder CheckRegistration(ServiceLifetime lifetime, Type implementationType)
        {
            return ThrowIfContainerBuilt()
                   .ThrowIfTransientDisposable(lifetime, implementationType)
                   .ThrowIfImplementationTypeUnappropriate(implementationType);
        }

        private DIContainerBuilder ThrowIfContainerBuilt()
        {
            if (isBuild)
            {
                throw new RegistrationServiceException("This container was built already");
            }
            return this;
        }

        private DIContainerBuilder ThrowIfTransientDisposable(ServiceLifetime lifetime, Type implementationType)
        {
            if(lifetime == ServiceLifetime.Transient && implementationType.GetInterface("IDisposable") != null)
            {
                throw new RegistrationServiceException("It is prohibited to register transient disposable service");
            }
            return this;
        }

        private DIContainerBuilder ThrowIfImplementationTypeUnappropriate(Type implementationType)
        {
            if (services.Any(service => service.Value == implementationType))
            {
                throw new RegistrationServiceException($"Service with type {implementationType.FullName} has been already registered");
            }
            return this;
        }
    }
}
