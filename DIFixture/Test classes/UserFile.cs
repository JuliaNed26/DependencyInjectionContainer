using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class UserFile : IUserFile
    {
        public string GetInfo()
        {
            return "This is a user file";
        }

        public bool TryOpen() => true;
    }
}
