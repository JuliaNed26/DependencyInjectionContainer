using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer;

using DIFixture.Test_classes;
using DIFixture.Test_classes.DisposableClasses;

namespace DIFixture.Fixtures;

internal class DiContainerBuilderRulesRegistrationFixture : BaseFixture
{
    //Rules on second registration of the same service

    [Test]
    public void RegisterWithSecondRegistrationActionIgnore_SecondRegistration_ShouldIgnoreSecondRegistration()
    {
        // Arrange
        Builder = new DiContainerBuilder(default, SecondRegistrationAction.Ignore);
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        // Act
        Builder.RegisterWithImplementation(obj1, ServiceLifetime.Singleton);
        Builder.RegisterWithImplementation(obj2, ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Assert
        Assert.That(container.Resolve<FileLogger>(), Is.SameAs(obj1));
        Assert.That(container.Resolve<FileLogger>(), Is.Not.SameAs(obj2));
    }

    [Test]
    public void RegisterWithSecondRegistrationActionRewrite_SecondRegistration_ShouldRewriteIntoSecondRegistration()
    {
        // Arrange
        Builder = new DiContainerBuilder(default, SecondRegistrationAction.Rewrite);
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        // Act
        Builder.RegisterWithImplementation(obj1, ServiceLifetime.Singleton);
        Builder.RegisterWithImplementation(obj2, ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Assert
        Assert.That(container.Resolve<FileLogger>(), Is.SameAs(obj2));
        Assert.That(container.Resolve<FileLogger>(), Is.Not.SameAs(obj1));
    }


    [Test]
    public void RegisterWithSecondRegistrationActionThrow_SecondRegistration_ShouldThrowRegistrationServiceException()
    {
        // Arrange
        Builder = new DiContainerBuilder();
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        Builder.RegisterWithImplementation(obj1, ServiceLifetime.Singleton);
        // Act
        // Assert
        Assert.Throws<RegistrationServiceException>(() =>
            Builder.RegisterWithImplementation(obj2, ServiceLifetime.Singleton));
    }

    //Rules on registration with many constructors

    [Test]
    public void RegisterWithManyConstructors_RegisterWithManyConstructorsWithFactory_GetCreatedByFactory()
    {
        // Arrange
        Builder = new DiContainerBuilder(Rules.GetConstructorWithMostRegisteredParameters);
        Builder.Register(ServiceLifetime.Transient, container => new ManyConstructors());
        var container = Builder.Build();
        // Act
        ManyConstructors resolved = container.Resolve<ManyConstructors>();
        // Assert
        Assert.That(resolved.ConstructorUsed, Is.EqualTo("Without parameters"));
    }

    [Test]
    public void RegisterWithManyConstructors_RuleGetConstructorWithMostRegisteredParameters_ResolveWithMostAppropriateConstructor()
    {
        // Arrange
        Builder = new DiContainerBuilder(Rules.GetConstructorWithMostRegisteredParameters);
        Builder.Register<ManyConstructors>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Act
        ManyConstructors resolved = container.Resolve<ManyConstructors>();
        // Assert
        Assert.That(resolved.ConstructorUsed, Is.EqualTo("With IErrorLogger"));
    }

    //Rules on transient disposable registration 

    [Test]
    public void RegisterTransientDisposable_RuleDisposeTransientWhenDisposeContainer_DisposeTransientServicesWithSingleton()
    {
        // Arrange
        Builder = new DiContainerBuilder(Rules.DisposeTransientWhenDisposeContainer);
        Builder.Register<ChildDisposableClass>(ServiceLifetime.Transient);
        Builder.Register<ParentDisposableClass>(ServiceLifetime.Singleton);
        Builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<ParentDisposableClass>();
        List<Type> expected = new List<Type>() { typeof(ParentDisposableClass), typeof(ChildDisposableClass) };
        // Act
        container.Dispose();
        // Assert
        CollectionAssert.AreEqual(expected, disposeSequence.GetDisposedClasses());
    }
}