using System.Collections;
using System.ComponentModel.Composition;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public class DIContainerBuilder
    {
        private List<Service> services;
        private DIContainer parent;

        public DIContainerBuilder()
        {
            services = new List<Service>();
            parent = null;
        }

        internal DIContainerBuilder(DIContainer _parent)
        {
            services = new List<Service>();
            parent = _parent;
        }

        public void Register<TIType, TImplementation>(ServiceLifetime lifetime) where TImplementation : TIType
        {
            ThrowIfTypeUnappropriate(typeof(TImplementation));
            services.Add(new Service(typeof(TImplementation), lifetime, typeof(TIType)));
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
            return new DIContainer(services, parent);
        }

        private void ThrowIfTypeUnappropriate(Type type)
        {
            var ctorsWithImportAttribute = type.GetConstructors()
                    .Where(constructor => constructor.GetCustomAttribute<ImportingConstructorAttribute>() != null);

            if (type.GetConstructors().Count() > 1 && 
                ( ctorsWithImportAttribute.Count() > 1 || ctorsWithImportAttribute.Count() == 0))
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
