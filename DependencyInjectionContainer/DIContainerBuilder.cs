using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public sealed class DIContainerBuilder
    {
        private List<Service> services;
        private DIContainer parentContainer;
        private DIContainer constructedContainer;
        private bool isBuild;

        public DIContainerBuilder()
        {
            services = new List<Service>();
            isBuild = false;
        }

        internal DIContainerBuilder(DIContainer parent) : this() => parentContainer = parent;

        public void Register<TImplementationInterface, TImplementation>(ServiceLifetime lifetime) 
                            where TImplementation : TImplementationInterface
        {
            ThrowIfContainerBuilt();
            ThrowIfImplementationTypeUnappropriate(typeof(TImplementation));

            services.Add(new Service(typeof(TImplementationInterface), typeof(TImplementation), lifetime));
        }

        public void Register<TImplementation>(ServiceLifetime lifetime)
                            where TImplementation : class
        {
            if(typeof(TImplementation).IsAbstract)
            {
                throw new ArgumentException("Can't register type without assigned implementation type");
            }
            ThrowIfContainerBuilt();
            ThrowIfImplementationTypeUnappropriate(typeof(TImplementation));

            services.Add(new Service(typeof(TImplementation), lifetime));
        }

        public void RegisterWithImplementation(object implementation, ServiceLifetime lifetime)
        {
            ThrowIfContainerBuilt();
            ThrowIfImplementationTypeUnappropriate(implementation.GetType());

            services.Add(new Service(implementation, lifetime));
        }

        public void RegisterAssemblyByAttributes(Assembly assembly)
        {
            ThrowIfContainerBuilt();

            var typesWithRegisterAttribute = assembly
                                             .GetTypes()
                                             .Where(t => t.GetCustomAttribute<RegisterAttribute>() != null);

            foreach (var type in typesWithRegisterAttribute)
            {
                RegisterAttribute serviceInfo = type.GetCustomAttribute<RegisterAttribute>();
                ThrowIfImplementationTypeUnappropriate(type);
                services.Add(new Service(serviceInfo.InterfaceType, type, serviceInfo.Lifetime));
            }
        }

        public DIContainer Build()
        {
            if(!isBuild)
            {
                isBuild = true;
                constructedContainer = new DIContainer(services, parentContainer);
            }
            return constructedContainer;
        }

        private void ThrowIfContainerBuilt()
        {
            if (isBuild)
            {
                throw new ArgumentException("This container was built already");
            }
        }

        private void ThrowIfImplementationTypeUnappropriate(Type implementationType)
        {
            if (IsServiceWithImplementationTypeRegistered())
            {
                throw new ArgumentException($"Service with type {implementationType.FullName} has been already registered");
            }

            if (implementationType.GetConstructors().Count() > 1)
            {
                throw new ArgumentException("Can't register type with more than one constructor");
            }

            bool IsServiceWithImplementationTypeRegistered()
            {
                if (services.Any(service => service.ImplementationType == implementationType))
                {
                    return true;
                }

                DIContainer curContainer = parentContainer;
                while(curContainer != null)
                {
                    if (curContainer.IsServiceRegistered(implementationType))
                    {
                        return true;
                    }
                    curContainer = curContainer.ContainerParent;
                }
                return false;
            }
        }
    }
}
