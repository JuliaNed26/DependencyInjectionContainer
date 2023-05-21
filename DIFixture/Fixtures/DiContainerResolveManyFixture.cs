using DependencyInjectionContainer.Enums;

using DIFixture.Test_classes;

namespace DIFixture.Fixtures;

internal class DiContainerResolveManyFixture : BaseFixture
{
    [Test]
    public void ResolveMany_ByInterfaceWhenTwoTypesImplementsIt_ShouldGetEnumerableOfServices()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        var resolved = container.ResolveMany<IErrorLogger>().ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    //ResolveMany with child containers

    [Test]
    public void ResolveMany_Local_ShouldGetTypesOnlyFromACurrentContainer()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local).ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void ResolveMany_LocalWhenNotRegisteredInLocalContainerButRegisteredInParent_ShouldReturnEmptyIEnumerable()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        // Act
        // Assert
        Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local).Count(), Is.EqualTo(0));
    }

    [Test]
    public void ResolveMany_NonLocal_ShouldGetTypesOnlyFromAParentContainer()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal).ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void ResolveMany_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldReturnEmptyIEnumerable()
    {
        // Arrange
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal).Count(), Is.EqualTo(0));
    }

    [Test]
    public void ResolveMany_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<NullReferenceException>(() => container.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void ResolveMany_AnyWhenTypeImplementsInterfaceExistsInCurrentContainerAndInParent_GetFromBoth()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        var resolved = childContainer.ResolveMany<IErrorLogger>().ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    [Test]
    public void ResolveMany_AnyWhenServicesOfSameTypeExistsInCurrentContainerAndInParent_GetFromChild()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        var resolved = childContainer.ResolveMany<IErrorLogger>().ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.IsTrue(ReferenceEquals(resolved.Single(), childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local)));
        Assert.IsFalse(ReferenceEquals(resolved.Single(), parentContainer.Resolve<IErrorLogger>(ResolveStrategy.Local)));
    }
}