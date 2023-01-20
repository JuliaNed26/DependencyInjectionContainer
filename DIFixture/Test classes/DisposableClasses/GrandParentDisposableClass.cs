using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal class GrandParentDisposableClass : DisposableClass
    {
        public GrandParentDisposableClass(ParentDisposableClass childClass, List<Type> disposeSequence)
        {
            Child = childClass;
            IsDisposed = false;
            _disposeSequence = disposeSequence;
        }
        public ParentDisposableClass Child { get; init; }
    }
}
