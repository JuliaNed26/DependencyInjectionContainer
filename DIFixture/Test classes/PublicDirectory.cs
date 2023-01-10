using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    [Register(ServiceLifetime.Transient,typeof(IUserDirectory))]
    internal sealed class PublicDirectory : IUserDirectory
    {
        public string GetInfo()
        {
            return "This is a public directory";
        }
    }
}
