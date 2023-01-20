using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer.Exceptions
{
    public sealed class RegistrationServiceException: Exception
    {
        public RegistrationServiceException(string message) : base(message) { }
    }
}
