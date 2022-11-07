namespace DependencyInjectionContainer.Exceptions
{
    public sealed class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(Type serviceType)
            :base($"Service with type {serviceType.FullName} was not found") { }
    }
}
