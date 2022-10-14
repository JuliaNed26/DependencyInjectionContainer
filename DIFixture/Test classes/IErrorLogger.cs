using DependencyInjectionContainer;
using DependencyInjectionContainer.Attributes;

namespace DIFixture.Test_classes
{
    internal interface IErrorLogger
    {
        string Log(string message);
    }
}