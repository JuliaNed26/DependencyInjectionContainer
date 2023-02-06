namespace DIFixture.Test_classes;

internal sealed class GrandParentDisposableClass : DisposableClass
{
    private ParentDisposableClass child;
    public GrandParentDisposableClass(ParentDisposableClass childClass, DisposableRegistrator registrator)
    {
        child = childClass;
        disposableRegistrator = registrator;
    }
}

