using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;

namespace DIFixture.Test_classes
{
    [Register(typeof(IErrorLogger), ServiceLifetime.Singleton)]
    internal sealed class ConsoleLogger : IErrorLogger
    {
        public string Log(string message)
        {
            return $"Logged into console with message: {message}";
        }
    }
}
