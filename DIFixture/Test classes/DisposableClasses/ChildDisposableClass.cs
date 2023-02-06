namespace DIFixture.Test_classes;

internal sealed class ChildDisposableClass : DisposableClass
{
    public ChildDisposableClass(DisposableRegistrator registrator)
    {
        disposableRegistrator = registrator;
    }
}

