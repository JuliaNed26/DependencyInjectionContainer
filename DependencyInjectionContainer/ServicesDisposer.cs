namespace DependencyInjectionContainer;
using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class ServicesDisposer : IDisposable
{
    private HashSet<object> instances = new HashSet<object>();

    public void Dispose()
    {
        for(int i = instances.Count - 1; i >= 0; i--)
        {
            if(instances.ElementAt(i) is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        instances.Clear();
    }

    internal void Add(IDisposable instance) => instances.Add(instance);
}
