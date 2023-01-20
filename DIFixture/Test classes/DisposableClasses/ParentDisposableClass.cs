using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal class ParentDisposableClass : DisposableClass
    {
        public ParentDisposableClass(ChildDisposableClass childClass, List<Type> disposeSequence)
        {
            Child = childClass;
            IsDisposed = false;
            _disposeSequence = disposeSequence;
        }
        public ChildDisposableClass Child { get; init; }
    }
}
