using DIFixture.Test_classes.DisposableClasses;
namespace DIFixture;
using DependencyInjectionContainer;
using Test_classes;
using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer.Enums;

public class DiContainerBuilderFixture
{
    private DiContainerBuilder builder = new();

    [SetUp]
    public void Setup()
    {
        builder = new DiContainerBuilder();
    }

    [Test]
    public void Register_TwoRegistrationsWithEqualKeyValueTypesInContainer_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient);
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient));
        var obj = new FileLogger();
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation<IErrorLogger>(obj, LifetimeOfService.Singleton));

        builder.Register<FileLogger>(LifetimeOfService.Transient);
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, LifetimeOfService.Singleton));
    }

    [Test]
    public void Register_ByInterfaceOnly_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger>(LifetimeOfService.Singleton));
    }

    [Test]
    public void RegisterWithImplementation_ShouldResolveByImplementationType()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, LifetimeOfService.Singleton);
        using var container = builder.Build();
        Assert.That((IErrorLogger)container.Resolve<FileLogger>(), Is.EqualTo(logger));
    }

    [Test]
    public void RegisterWithImplementation_ResolveByInterfaceType_ShouldThrowServiceNotFoundException()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, LifetimeOfService.Singleton);
        using var container = builder.Build();
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
    }

    [Test]
    public void Register_RegisterAfterBuild_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient);
        using var container = builder.Build();
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(LifetimeOfService.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ConsoleLoggerWithAttribute>(LifetimeOfService.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(LifetimeOfService.Transient));
        var obj = new ConsoleLoggerWithAttribute();
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, LifetimeOfService.Singleton));
    }

    [Test]
    public void Build_TheSecondBuild_ShouldThrowInvalidOperationException()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient);
        using var container = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void RegisterByAssembly_ShouldGetOnlyTypesWithRegisterAttributeWhenResolve()
    {
        builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
        using var container = builder.Build();
        Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
        Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectoryWithAttribute)));
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
    }

    [Test]
    public void Register_RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Singleton);
        using var container = builder.Build();
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        Assert.IsTrue(ReferenceEquals(obj1, obj2));
    }

    [Test]
    public void Register_RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient);
        using var container = builder.Build();
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        Assert.IsFalse(ReferenceEquals(obj1, obj2));
    }

    [Test]
    public void Register_RegisterImplementationTypeInAChildContainerWhenItExistsInParent_ShouldOverrideParentsRegistration()
    {
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Singleton);
        using var container = builder.Build();
        var childBuilder = container.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Transient);
        using var childContainer = childBuilder.Build();
        Assert.IsFalse(ReferenceEquals(container.Resolve<IErrorLogger>(), childContainer.Resolve<IErrorLogger>()));
        
    }

    [Test]
    public void RegisterWithImplementation_WithInterfaceWhenImplementationDoNotImplementIt_ShouldThrowArgumentException()
    {
        var obj = new FileLogger();
        Assert.Throws<ArgumentException>(() => builder.RegisterWithImplementation<IUserDirectory>(obj, LifetimeOfService.Transient));
    }

    [Test]
    public void RegisterWithSecondRegistrationActionIgnore_SecondRegistration_ShouldIgnoreSecondRegistration()
    {
        builder = new DiContainerBuilder(default, SecondRegistrationAction.Ignore);
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        builder.RegisterWithImplementation(obj1,LifetimeOfService.Singleton);
        builder.RegisterWithImplementation(obj2, LifetimeOfService.Singleton);
        var container = builder.Build();
        Assert.That(container.Resolve<FileLogger>(),Is.SameAs(obj1));
        Assert.That(container.Resolve<FileLogger>(), Is.Not.SameAs(obj2));
    }

    [Test]
    public void RegisterWithSecondRegistrationActionRewrite_SecondRegistration_ShouldRewriteIntoSecondRegistration()
    {
        builder = new DiContainerBuilder(default,SecondRegistrationAction.Rewrite);
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        builder.RegisterWithImplementation(obj1, LifetimeOfService.Singleton);
        builder.RegisterWithImplementation(obj2, LifetimeOfService.Singleton);
        var container = builder.Build();
        Assert.That(container.Resolve<FileLogger>(), Is.SameAs(obj2));
        Assert.That(container.Resolve<FileLogger>(), Is.Not.SameAs(obj1));
    }


    [Test]
    public void RegisterWithSecondRegistrationActionThrow_SecondRegistration_ShouldThrowRegistrationServiceException()
    {
        builder = new DiContainerBuilder();
        var obj1 = new FileLogger();
        var obj2 = new FileLogger();
        builder.RegisterWithImplementation(obj1, LifetimeOfService.Singleton);
        Assert.Throws<RegistrationServiceException>(() =>
            builder.RegisterWithImplementation(obj2, LifetimeOfService.Singleton));
    }

    [Test]
    public void Register_RegisterTypeWithManyConstructorsNotDefineWhichToUse_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ManyConstructors>(LifetimeOfService.Singleton));
    }

    [Test]
    public void RegisterWithManyConstructors_RegisterWithManyConstructorsWithFactory_GetCreatedByFactory()
    {
        builder = new DiContainerBuilder(Rules.GetConstructorWithMostRegisteredParameters);
        builder.Register<ManyConstructors>(LifetimeOfService.Transient, container => new ManyConstructors());
        var container = builder.Build();
        ManyConstructors resolved = container.Resolve<ManyConstructors>();
        Assert.That(resolved.ConstructorUsed, Is.EqualTo("Without parameters"));
    }

    [Test]
    public void RegisterWithManyConstructors_RuleGetConstructorWithMostRegisteredParameters_ResolveWithMostAppropriateConstructor()
    {
        builder = new DiContainerBuilder(Rules.GetConstructorWithMostRegisteredParameters);
        builder.Register<ManyConstructors>(LifetimeOfService.Singleton);
        builder.Register<IErrorLogger, FileLogger>(LifetimeOfService.Singleton);
        var container = builder.Build();
        ManyConstructors resolved = container.Resolve<ManyConstructors>();
        Assert.That(resolved.ConstructorUsed,Is.EqualTo("With IErrorLogger"));
    }

    [Test]
    public void RegisterTransientDisposable_RuleDisposeTransientWhenDisposeContainer_DisposeTransientServicesWithSingleton()
    {
        builder = new DiContainerBuilder(Rules.DisposeTransientWhenDisposeContainer);
        builder.Register<ChildDisposableClass>(LifetimeOfService.Transient);
        builder.Register<ParentDisposableClass>(LifetimeOfService.Singleton);
        builder.Register<DisposableSequence>(LifetimeOfService.Singleton); 
        var container = builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<ParentDisposableClass>(); 
        container.Dispose();
        List<Type> expected = new List<Type>() { typeof(ParentDisposableClass), typeof(ChildDisposableClass) };
        CollectionAssert.AreEqual(expected, disposeSequence.GetDisposedClasses());
    }

    [Test]
    public void RegisterTransientDisposable_ActionOnTransientDisposableIsThrow_ShouldThrowRegistrationServiceException()
    {
        builder = new DiContainerBuilder();
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ChildDisposableClass>(LifetimeOfService.Transient));
    }

}