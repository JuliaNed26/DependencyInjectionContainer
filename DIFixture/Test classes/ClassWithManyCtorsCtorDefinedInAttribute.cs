using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DependencyInjectionContainer.Attributes;
using DependencyInjectionContainer.Enums;

namespace DIFixture.Test_classes
{
   
    internal class ClassWithManyCtorsCtorDefinedInAttribute
    {
        IErrorLogger _errorLogger;
        IUserDirectory _userDirectory;
        public ClassWithManyCtorsCtorDefinedInAttribute(IErrorLogger errorLogger)
        {
            CtorUsed = "With IErrorLogger";
            _errorLogger = errorLogger;
        }
        public ClassWithManyCtorsCtorDefinedInAttribute(IUserDirectory userDirectory)
        {
            CtorUsed = "With IUserDirectory";
            _userDirectory = userDirectory;
        }

        public string CtorUsed { get; init; }
    }
}
