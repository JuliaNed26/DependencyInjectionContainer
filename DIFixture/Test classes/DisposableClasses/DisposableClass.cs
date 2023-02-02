using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal abstract class DisposableClass : IDisposable
    {
        protected DisposableRegistrator disposableRegistrator;
        public bool IsDisposed { get; protected set; }
        public void Dispose()
        {
            if (!IsDisposed)
            {
                disposableRegistrator.SaveDisposedClassType(this.GetType());
            }
            IsDisposed = true;
        }
    }
}
