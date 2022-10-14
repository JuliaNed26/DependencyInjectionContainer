using System.Collections;
using System.ComponentModel.Composition;
using System.Reflection;
using DependencyInjectionContainer.Attributes;

namespace DependencyInjectionContainer
{
    public sealed class DIContainerBuilder
    {
        private List<Service> services;
        private DIContainer parent;
        private bool isBuild;

        public DIContainerBuilder()
        {
            services = new List<Service>();
            parent = null;
            isBuild = false;
        }

        internal DIContainerBuilder(DIContainer _parent)
        {
            services = new List<Service>();
            parent = _parent;
            isBuild = false;
        }

        public void Register<TImplementationInterface, TImplementation>(ServiceLifetime lifetime) where TImplementation : TImplementationInterface
        {
            ThrowIfTypeUnappropriate(typeof(TImplementation));
            services.Add(new Service(typeof(TImplementation), lifetime, typeof(TImplementationInterface)));
        }

        public void Register<TImplementation>(ServiceLifetime lifetime) where TImplementation : class?
        {
            if(typeof(TImplementation).IsAbstract)
            {
                throw new ArgumentException("Can't register type without assigned implementation type");
            }
            ThrowIfTypeUnappropriate(typeof(TImplementation));
            services.Add(new Service(typeof(TImplementation), lifetime));
        }

        public void RegisterWithImplementation(object implementation, ServiceLifetime lifetime)
        {
            ThrowIfTypeUnappropriate(implementation.GetType());
            services.Add(new Service(implementation, lifetime));
        }

        public void RegisterAssemblyByAttributes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttribute<RegisterAttribute>() != null))
            {
                var attribute = type.GetCustomAttribute<RegisterAttribute>();
                ThrowIfTypeUnappropriate(type);
                services.Add(new Service(type, attribute.Lifetime, attribute.InterfaceType));
            }
        }

        public DIContainer Build()
        {
            isBuild = true;
            return new DIContainer(services, parent);
        }

        private void ThrowIfTypeUnappropriate(Type type)
        {
            if(parent != null && parent.Services.Where(service => service.ImplementationType == type).Any())
            {
                throw new ArgumentException($"Service with type {type.FullName} has been already registered");
            }
            if(isBuild)
            {
                throw new ArgumentException("This container was built already");
            }
            if (type.GetConstructors().Count() > 1)
            {
                throw new ArgumentException("Can't register type with more than one constructor");
            }

            bool containsServiceWithType = services.Any(service => service.ImplementationType == type);

            if (containsServiceWithType)
            {
                throw new ArgumentException($"Service with type {type.FullName} has been already registered");
            }
        }
    }
}
