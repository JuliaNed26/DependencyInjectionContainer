using System.Collections;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public class DIContainer
    {
        private List<Service> services;
        private bool isResolved;
        private DIContainer parent;

        public DIContainer()
        {
            services = new List<Service>();
            isResolved = false;
            parent = null;
        }

        private DIContainer(DIContainer _parent)
        {
            services = new List<Service>();
            isResolved = false;
            parent = _parent;
        }

        public DIContainer CreateAChildContainer()
        {
            return new DIContainer(this);
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

        public TResolveType Resolve<TResolveType>() where TResolveType : class?
        {
            return (TResolveType)Resolve(typeof(TResolveType));
        }

        public IEnumerable<TResolveType> ResolveMany<TResolveType>(bool includeParent = false) where TResolveType : class?
        {
            var resolvedServices = services.Where(service => IsServiceOfGivenType(service, typeof(TResolveType)))
                                            .Select(service => (TResolveType)Resolve(service.ImplementationType)).ToList();
            if (parent != null && includeParent)
            {
                resolvedServices.AddRange(parent.ResolveMany<TResolveType>(includeParent));
            }

            return resolvedServices;
        }

        private object Resolve(Type typeToResolve)
        {
            var servicesToResolve = services.Where(service => IsServiceOfGivenType(service, typeToResolve));

            if(servicesToResolve.Count() > 1)
            {
                throw new ArgumentException($"Many services with type {typeToResolve} was registered. Use ResolveMany to resolve them all");
            }

            if (servicesToResolve.Count() == 0)
            {
                if (parent != null)
                {
                    return parent.Resolve(typeToResolve);
                }
                else
                {
                    throw new NullReferenceException($"Do not have registrated services of type {typeToResolve.FullName}");
                }
            }

            isResolved = true;

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
                var parameters = service.ImplementationType.GetConstructors().First().
                                 GetParameters().Select(parameter => Resolve(parameter.ParameterType)).ToArray();

                return Activator.CreateInstance(service.ImplementationType, parameters);
            }
        }

        private void ThrowIfTypeUnappropriate(Type type)
        {
            if(isResolved)
            {
                throw new ArgumentException("Can't register after resolving. Create a child container");
            }
            if(type.GetConstructors().Count() > 1)
            {
                throw new ArgumentException("Can't register type with more than one constructor");
            }
            bool containsServiceWithType = services.Any(service => service.ImplementationType == type);
            if (containsServiceWithType)
            {
                throw new ArgumentException($"Service with type {type.FullName} has been already registered");
            }
            int i = type.GetConstructors(BindingFlags.Public).Count();
            if (type.GetConstructors().Count() > 1)
            {
                throw new ArgumentException();
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
