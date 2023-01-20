using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal abstract class DisposableClass : IDisposable
    {
        protected List<Type> _disposeSequence;
        public bool IsDisposed { get; protected set; }
        public void Dispose()
        {
            if (!IsDisposed)
            {
                _disposeSequence.Add(this.GetType());
            }
            IsDisposed = true;
        }
    }
}
