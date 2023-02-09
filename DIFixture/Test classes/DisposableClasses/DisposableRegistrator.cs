﻿namespace DIFixture.Test_classes;

internal sealed class DisposableRegistrator
{
    private List<Type> disposedItems = new List<Type>();

    public void SaveDisposedClassType(Type disposedType) => disposedItems.Add(disposedType);
    public IEnumerable<Type> GetDisposedClasses() => disposedItems;
}

