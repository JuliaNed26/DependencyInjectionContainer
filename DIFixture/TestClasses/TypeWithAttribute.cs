using DependencyInjectionContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    [Register(typeof(TypeWithAttribute),ServiceLifetime.Singleton)]
    public class TypeWithAttribute
    {
        int k;
        public TypeWithAttribute() 
        {
        }
    }
}
