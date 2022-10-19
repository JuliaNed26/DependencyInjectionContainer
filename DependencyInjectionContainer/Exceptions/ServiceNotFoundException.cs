using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer.Exceptions
{
    public sealed class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(Type serviceType)
            :base($"Service with type {serviceType.FullName} was not found") { }
    }
}
