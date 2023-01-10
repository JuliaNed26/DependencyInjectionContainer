using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class SystemFile : IUserFile
    {
        public string GetInfo()
        {
            return "This is a system file";
        }

        public bool TryOpen() => false;
    }
}
