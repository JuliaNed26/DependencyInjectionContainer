using DependencyInjectionContainer;
using DependencyInjectionContainer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    [Register(ServiceLifetime.Singleton,typeof(IErrorLogger))]
    internal sealed class ConsoleLogger : IErrorLogger
    {
        public string Log(string message)
        {
            return $"Logged into console with message: {message}";
        }
    }
}
