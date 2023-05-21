using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;

using DIFixture.Test_classes;
using DIFixture.Test_classes.DisposableClasses;

namespace DIFixture.Fixtures;

internal class DiContainerBuilderRegistrationFixture : BaseFixture
{
    [Test]
    public void Register_ByInterfaceOnly_ShouldThrowRegistrationServiceException()
        => Assert.Throws<RegistrationServiceException>(() => Builder.Register<IErrorLogger>(ServiceLifetime.Singleton));

    [Test]
    public void Register_RegisterAfterBuild_ShouldThrowRegistrationServiceException()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = Builder.Build();
        var obj = new ConsoleLoggerWithAttribute();
        // Act
        // Assert
        Assert.Throws<RegistrationServiceException>(() => Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => Builder.Register<ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient));
        Assert.Throws<RegistrationServiceException>(() => Builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
    }

    [Test]
    public void Register_RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        // Assert
        Assert.IsTrue(ReferenceEquals(obj1, obj2));
    }

    [Test]
    public void Register_RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = Builder.Build();
        // Act
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        // Assert
        Assert.IsFalse(ReferenceEquals(obj1, obj2));
    }

    //Register with child containers

    [Test]
    public void Register_RegisterImplementationTypeInAChildContainerWhenItExistsInParent_ShouldOverrideParentsRegistration()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        var childBuilder = container.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.IsFalse(ReferenceEquals(container.Resolve<IErrorLogger>(), childContainer.Resolve<IErrorLogger>()));

    }

    //Check default work of container Builder

    [Test]
    public void Register_TwoRegistrationsWithEqualKeyValueTypesInContainer_ShouldThrowRegistrationServiceException()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient); 
        var obj = new FileLogger();
        Builder.Register<FileLogger>(ServiceLifetime.Transient);
        // Act
        // Assert
        Assert.Throws<RegistrationServiceException>(() => Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
        Assert.Throws<RegistrationServiceException>(() => Builder.RegisterWithImplementation<IErrorLogger>(obj, ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => Builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
    }

    [Test]
    public void Register_RegisterTypeWithManyConstructorsNotDefineWhichToUse_ShouldThrowRegistrationServiceException()
        => Assert.Throws<RegistrationServiceException>(() => Builder.Register<ManyConstructors>(ServiceLifetime.Singleton));

    [Test]
    public void RegisterTransientDisposable_ActionOnTransientDisposableIsThrow_ShouldThrowRegistrationServiceException() 
        => Assert.Throws<RegistrationServiceException>(() => Builder.Register<ChildDisposableClass>(ServiceLifetime.Transient));

    //Register with attributes

    [Test]
    public void RegisterByAssembly_ShouldGetOnlyTypesWithRegisterAttributeWhenResolve()
    {
        // Arrange
        Builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
        Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectoryWithAttribute)));
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
    }

    //Register with implementation

    [Test]
    public void RegisterWithImplementation_ResolveByInterfaceType_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        IErrorLogger logger = new FileLogger();
        Builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
    }

    [Test]
    public void RegisterWithImplementation_ShouldResolveByImplementationType()
    {
        // Arrange
        IErrorLogger logger = new FileLogger();
        Builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.That((IErrorLogger)container.Resolve<FileLogger>(), Is.EqualTo(logger));
    }

    [Test]
    public void RegisterWithImplementation_WithInterfaceWhenImplementationDoNotImplementIt_ShouldThrowArgumentException()
    {
        // Arrange
        var obj = new FileLogger();
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => Builder.RegisterWithImplementation<IUserDirectory>(obj, ServiceLifetime.Transient));
    }
}