namespace DependencyInjectionContainer;
using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class ServicesDisposer : IDisposable
{
    private readonly List<object> instances = new();

    public async void Dispose()
    {
        for(var i = instances.Count - 1; i >= 0; i--)
        {
            if(instances.ElementAt(i) is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (instances.ElementAt(i) is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(true);
            }
        }
        instances.Clear();
    }

    public void Add(IDisposable instance) => instances.Add(instance);
    public void Add(IAsyncDisposable instance) => instances.Add(instance);
}
