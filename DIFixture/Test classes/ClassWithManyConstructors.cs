﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class ClassWithManyConstructors
    {
        IErrorLogger errorLogger;
        public ClassWithManyConstructors() { }
        public ClassWithManyConstructors(IErrorLogger _errorLogger) 
        { 
            errorLogger = _errorLogger;
        }
    }
}