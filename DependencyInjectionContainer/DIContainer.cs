using System.Collections;
using System.Reflection;

namespace DependencyInjectionContainer
{
    public class DIContainer
    {
        private List<Service> services;

        public DIContainer(Assembly currAssembly)
        {
            services = new List<Service>();
            RegisterWithAttributes(currAssembly);
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

        public void Register(object implementation, ServiceLifetime lifetime)
        {
            ThrowIfTypeUnappropriate(implementation.GetType());
            services.Add(new Service(implementation, lifetime));
        }

        public TResolveType Resolve<TResolveType>() where TResolveType : class?
        {
            return (TResolveType)Resolve(typeof(TResolveType));
        }

        public IEnumerable<TResolveType> ResolveMany<TResolveType>() where TResolveType : class?
        {
            return services.Where(service => IsServiceOfGivenType(service, typeof(TResolveType)))
                                            .Select(service => (TResolveType)Resolve(service.ImplementationType)).ToArray();
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
            //simplify
            object GetCreatedImplementationForService(Service service)
            {
                var parameters = service.ImplementationType.GetConstructors().First().
                                 GetParameters().Select(parameter => Resolve(parameter.ParameterType)).ToArray();

                return Activator.CreateInstance(service.ImplementationType, parameters);
            }
        }
        private void RegisterWithAttributes(Assembly curAssembly)
        {
            foreach (var type in curAssembly.GetTypes().Where(t => t.GetCustomAttribute<RegisterAttribute>() != null))
            {
                var attribute = type.GetCustomAttribute<RegisterAttribute>();
                ThrowIfTypeUnappropriate(attribute.ImplementType);
                services.Add(new Service(attribute.ImplementType, attribute.Lifetime, attribute.InterfaceType));
            }
        }

        private void ThrowIfTypeUnappropriate(Type type)
        {
            bool containsServiceWithType = services.Any(service => service.ImplementationType == type);
            if (containsServiceWithType)
            {
                throw new ArgumentException($"Service with type {type.FullName} has been already registered");
            }
            int i = type.GetConstructors(BindingFlags.Public).Count();
            if (type.GetConstructors().Count() != 1)
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
