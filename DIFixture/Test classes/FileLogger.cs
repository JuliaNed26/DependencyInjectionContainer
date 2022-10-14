using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class FileLogger : IErrorLogger
    {
        public string Log(string message)
        {
            return $"Logged into a file with message: {message}";
        }
    }
}
