﻿using DependencyInjectionContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    [Register(ServiceLifetime.Singleton,typeof(ITypeWithAttribute))]
    public class TypeWithAttribute : ITypeWithAttribute
    {
        public void SomeMethod()
        {
            Console.WriteLine("Some method");
        }
    }
}
