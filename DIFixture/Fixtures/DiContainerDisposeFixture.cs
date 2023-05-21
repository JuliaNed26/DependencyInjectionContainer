using DependencyInjectionContainer.Enums;

using DIFixture.Test_classes;
using DIFixture.Test_classes.DisposableClasses;

namespace DIFixture.Fixtures;

internal class DiContainerDisposeFixture : BaseFixture
{
    [Test]
    public void Dispose_ResolveSingletonServiceTwice_ShouldDisposeOneTime()
    {
        // Arrange
        Builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        Builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<ChildDisposableClass>();
        container.Resolve<ChildDisposableClass>();
        // Act
        container.Dispose();
        // Assert
        Assert.That(disposeSequence.GetDisposedClasses().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Dispose_DisposeSecondTime_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var container = Builder.Build();
        // Act
        container.Dispose();
        // Assert
        Assert.Throws<InvalidOperationException>(() => container.Dispose());
    }

    [Test]
    public void Dispose_ResolveAfterDispose_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Act
        container.Dispose();
        // Assert
        Assert.Throws<InvalidOperationException>(() => container.Resolve<ChildDisposableClass>());
    }

    //Dispose containers hierarchy

    [Test]
    public void Dispose_ChildContainerDisposed_ParentContainerShouldNotBeDisposed()
    {
        // Arrange
        Builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Singleton);
        using (var parentContainer = Builder.Build())
        {
            var childContainerBuilder = parentContainer.CreateChildContainer();
            var childContainer = childContainerBuilder.Build();
            // Act
            childContainer.Dispose();
            // Assert
            Assert.Throws<InvalidOperationException>(() => childContainer.Dispose());
            Assert.That(parentContainer.Resolve<IUserDirectory>(), Is.Not.Null);
        }
    }

    [Test]
    public void Dispose_ShouldBeDisposedStartingFromParentToChildClass()
    {
        // Arrange
        Builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        Builder.Register<GrandParentDisposableClass>(ServiceLifetime.Singleton);
        Builder.Register<ParentDisposableClass>(ServiceLifetime.Singleton);
        Builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<GrandParentDisposableClass>();
        List<Type> expected = new List<Type>()
            { typeof(GrandParentDisposableClass), typeof(ParentDisposableClass), typeof(ChildDisposableClass) };
        // Act
        container.Dispose();
        // Assert
        CollectionAssert.AreEqual(expected, disposeSequence.GetDisposedClasses());
    }

    //Dispose IAsyncDisposable

    [Test]
    public void Dispose_DisposeWithIAsyncDisposable()
    {
        // Arrange
        Builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        Builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        Builder.Register<DisposableAsyncClass>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<DisposableAsyncClass>();
        container.Resolve<ChildDisposableClass>();
        List<Type> expected = new List<Type>() { typeof(DisposableAsyncClass), typeof(ChildDisposableClass) };
        // Act
        container.Dispose();
        // Assert
        CollectionAssert.AreEquivalent(expected, disposeSequence.GetDisposedClasses());
    }
}
