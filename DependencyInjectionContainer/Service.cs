namespace DependencyInjectionContainer
{
    internal sealed record Service
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