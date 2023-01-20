using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjectionContainer
{
    internal sealed class ServicesInstanceList : IDisposable
    {
        private List<object> instances;

        internal ServicesInstanceList()
        {
            instances = new List<object>();
        }

        public void Dispose()
        {
            for(int i = instances.Count - 1; i >= 0; i--)
            {
                if(instances[i] is IDisposable)
                {
                    (instances[i] as IDisposable).Dispose();
                }
            }
            instances = null;
            GC.SuppressFinalize(this);
        }

        internal void Add(object instance)
        {
            if (instances.Any(item => ReferenceEquals(item, instance)))
            {
                instances.Add(instance);
            }
        }
    }
}
