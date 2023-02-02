namespace DependencyInjectionContainer;
using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class ServicesInstanceList : IDisposable
{
    private List<object> instances = new List<object>();

    public void Dispose()
    {
        for(int i = instances.Count - 1; i >= 0; i--)
        {
            if(instances[i] is IDisposable)
            {
                (instances[i] as IDisposable).Dispose();
            }
        }
        instances.Clear();
    }

    internal void Add(object instance)
    {
        if (instances.All(item => !ReferenceEquals(item, instance)))
        {
            instances.Add(instance);
        }
    }
}
