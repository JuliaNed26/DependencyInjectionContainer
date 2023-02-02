using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class GrandParentDisposableClass : DisposableClass
    {
        public GrandParentDisposableClass(ParentDisposableClass childClass, DisposableRegistrator registrator)
        {
            Child = childClass;
            disposableRegistrator = registrator;
        }
        public ParentDisposableClass Child { get; init; }
    }
}
