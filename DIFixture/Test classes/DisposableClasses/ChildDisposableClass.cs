using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class ChildDisposableClass : DisposableClass
    {
        public ChildDisposableClass(DisposableRegistrator registrator)
        {
            disposableRegistrator = registrator;
        }
    }
}
