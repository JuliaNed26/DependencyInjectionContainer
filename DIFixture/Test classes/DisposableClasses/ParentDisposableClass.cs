using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class ParentDisposableClass : DisposableClass
    {
        public ParentDisposableClass(ChildDisposableClass childClass, DisposableRegistrator registrator)
        {
            Child = childClass;
            disposableRegistrator = registrator;
        }
        public ChildDisposableClass Child { get; init; }
    }
}
