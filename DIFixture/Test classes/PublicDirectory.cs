using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;

namespace DIFixture.Test_classes
{
    [Register(typeof(IUserDirectory), ServiceLifetime.Transient)]
    internal sealed class PublicDirectory : IUserDirectory
    {
        public string GetInfo()
        {
            return "This is a public directory";
        }
    }
}
