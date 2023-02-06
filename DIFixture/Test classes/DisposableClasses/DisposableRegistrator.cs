namespace DIFixture.Test_classes;

internal sealed class DisposableRegistrator
{
    private List<Type> disposedItems = new List<Type>();

    internal void SaveDisposedClassType(Type disposedType) => disposedItems.Add(disposedType);
    internal IEnumerable<Type> GetDisposedClasses() => disposedItems;
}

