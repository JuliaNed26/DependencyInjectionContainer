namespace DIFixture.Test_classes;

internal sealed class ParentDisposableClass : DisposableClass
{
    private ChildDisposableClass child;
    public ParentDisposableClass(ChildDisposableClass childClass, DisposableRegistrator registrator)
    {
        child = childClass;
        disposableRegistrator = registrator;
    }
}

