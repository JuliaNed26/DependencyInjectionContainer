using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;

namespace DIFixture.Test_classes
{
    [Register(ServiceLifetime.Transient, typeof(IUserDirectory))]
    internal sealed class PublicDirectoryWithAttribute : IUserDirectory
    {
        public string GetInfo()
        {
            return "This is a public directory";
        }
    }
}
