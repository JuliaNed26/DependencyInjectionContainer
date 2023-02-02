using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;
namespace DIFixture.Test_classes
{
    [Register(ServiceLifetime.Singleton,typeof(IErrorLogger))]
    internal sealed class ConsoleLoggerWithAttribute : IErrorLogger
    {
        public string Log(string message)
        {
            return $"Logged into console with message: {message}";
        }
    }
}
