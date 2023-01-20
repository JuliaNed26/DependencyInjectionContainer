using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class ClassWithManyConstructors
    {
        IErrorLogger _errorLogger;
        IUserDirectory _userDirectory;
        public ClassWithManyConstructors() => CtorUsed = "Parameterless";
        public ClassWithManyConstructors(IErrorLogger errorLogger) 
        {
            CtorUsed = "With IErrorLogger";
            _errorLogger = errorLogger;
        }
        public ClassWithManyConstructors(IUserDirectory userDirectory)
        {
            CtorUsed = "With IUserDirectory";
            _userDirectory = userDirectory;
        }

        public string CtorUsed { get; init; }
    }
}
