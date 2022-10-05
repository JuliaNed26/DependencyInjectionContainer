using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    internal class TypeWithManyCtorsWithoutAttributes
    {
        public TypeWithManyCtorsWithoutAttributes() { }
        public TypeWithManyCtorsWithoutAttributes(IProblem problem) { }
        public TypeWithManyCtorsWithoutAttributes(INotifier notifier) { }
    }
}
