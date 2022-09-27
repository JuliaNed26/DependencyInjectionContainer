using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    internal class TypeWithManyConstructors
    {
        public TypeWithManyConstructors() { }
        public TypeWithManyConstructors(IProblem problem) { }
        public TypeWithManyConstructors(INotifier notifier) { }
    }
}
