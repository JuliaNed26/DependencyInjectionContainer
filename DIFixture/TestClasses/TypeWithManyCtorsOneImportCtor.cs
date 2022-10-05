using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    internal class TypeWithManyCtorsOneImportCtor
    {
        public string CtorMessage { get; init; }

        [ImportingConstructor]
        public TypeWithManyCtorsOneImportCtor() 
        {
            CtorMessage = "import constructor";
        }
        public TypeWithManyCtorsOneImportCtor(IProblem problem)
        {
            CtorMessage = "not import constructor";
        }
        public TypeWithManyCtorsOneImportCtor(INotifier notifier)
        {
            CtorMessage = "not import constructor";
        }
    }
}
