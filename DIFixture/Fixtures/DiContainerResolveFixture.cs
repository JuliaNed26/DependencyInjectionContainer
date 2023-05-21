using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;

using DIFixture.Test_classes;

namespace DIFixture.Fixtures;

internal class DiContainerResolveFixture : BaseFixture
{
    [Test]
    public void Resolve_ServiceRegisteredByInterfaceResolveByImplementationType_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<ConsoleLoggerWithAttribute>());
    }

    [Test]
    public void Resolve_ComplexGraph_ShouldReturnImplementation()
    {
        // Arrange  
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        Builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        Builder.Register<IUserFile, SystemFile>(ServiceLifetime.Transient);
        Builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        Builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.That(container.Resolve<FileSystem>().GetType(), Is.EqualTo(typeof(FileSystem)));
    }

    [Test]
    public void Resolve_NotAllConstructorParametersWasRegistered_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        Builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        Builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<FileSystem>());
    }

    [Test]
    public void Resolve_TypeWithValueTypeParameterInConstructorRegisteredByImplementation_ShouldBeResolved()
    {
        // Arrange
        TypeWithIntParameter typeWithIntParameter = new TypeWithIntParameter(3);
        Builder.RegisterWithImplementation(typeWithIntParameter, ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.IsTrue(ReferenceEquals(typeWithIntParameter, container.Resolve<TypeWithIntParameter>()));
    }

    [Test]
    public void Resolve_TypeWithValueTypeParameterInConstructor_ShouldThrowArgumentException()
    {
        // Arrange
        Builder.Register<TypeWithIntParameter>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => container.Resolve<TypeWithIntParameter>());
    }

    [Test]
    public void Resolve_GenericTypeWhichWasRegisteredWithGenericType_ShouldResolveWithDefinedGenericType()
    {
        // Arrange
        Builder.Register<FileLogger>(ServiceLifetime.Singleton);
        Builder.Register<GenericClass<FileLogger>>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Act
        var resolved = container.Resolve<GenericClass<FileLogger>>();
        // Assert
        Assert.That(resolved.GetType(), Is.EqualTo(typeof(GenericClass<FileLogger>)));
    }

    [Test]
    public void Resolve_GenericTypeWhichWasRegisteredWithGenericType_ResolveWithAnotherGenericType_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        Builder.Register<FileLogger>(ServiceLifetime.Singleton);
        Builder.Register<GenericClass<FileLogger>>(ServiceLifetime.Singleton);
        var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<GenericClass<ConsoleLoggerWithAttribute>>());
    }

    //IEnumerable resolve 

    [Test]
    public void Resolve_ResolveIEnumerable_ShouldReturnEnumerableOfResolvedObjectsThatImplementsType()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        var resolved = container.Resolve<IEnumerable<IErrorLogger>>().ToList();
        // Assert
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    [Test]
    public void Resolve_ResolveClassImplementsIEnumerableWhichNotRegistered_ShouldThrowServiceNotFoundException()
    {
        // Arrange  
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<List<IErrorLogger>>());
    }

    // Check default work of container

    [Test]
    public void Resolve_HasManyServicesThatImplementConstructorParameterInterface_ShouldThrowResolveServiceException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        Builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        Builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        Builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ResolveServiceException>(() => container.Resolve<FileSystem>());
    }

    [Test]
    public void Resolve_ByInterfaceWhenTwoTypesImplementsItInOneContainer_ShouldThrowResolveServiceException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<ResolveServiceException>(() => container.Resolve<IErrorLogger>());
    }

    [Test]
    public void Resolve_ResolveServiceWithManyConstructorWhereImplementationFactoryWasDefined_ShouldResolveByFactory()
    {
        // Arrange
        ManyConstructors manyConstructors = new ManyConstructors();
        Builder.RegisterWithImplementation(manyConstructors, ServiceLifetime.Singleton);
        using var container = Builder.Build();

        var child1Builder = container.CreateChildContainer();
        child1Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient);
        child1Builder.Register(ServiceLifetime.Transient,
            usedContainer =>
            {
                var logger = usedContainer.Resolve<IErrorLogger>();
                return new ManyConstructors(logger);
            });
        using var child1Container = child1Builder.Build();

        var child2Builder = container.CreateChildContainer();
        child2Builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        child2Builder.Register(ServiceLifetime.Transient,
            usedContainer =>
            {
                var directory = usedContainer.Resolve<IUserDirectory>();
                return new ManyConstructors(directory);
            });
        using var child2Container = child2Builder.Build();
        // Act
        var resolved = container.Resolve<ManyConstructors>();
        var resolvedFromChild1 = child1Container.Resolve<ManyConstructors>();
        var resolvedFromChild2 = child2Container.Resolve<ManyConstructors>();
        // Assert
        Assert.That(resolved.ConstructorUsed, Is.EqualTo("Without parameters")); 
        Assert.That(resolvedFromChild1.ConstructorUsed, Is.EqualTo("With IErrorLogger"));
        Assert.That(resolvedFromChild2.ConstructorUsed, Is.EqualTo("With IUserDirectory"));
    }

    // Resolve using child containers

    [Test]
    public void Resolve_Local_ShouldGetTypeOnlyFromACurrentContainer()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local).GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void Resolve_LocalWhenNotRegisteredInLocalContainerButRegisteredInParent_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local));
    }

    [Test]
    public void Resolve_NonLocal_ShouldGetTypeOnlyFromAParentContainers()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert

        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void Resolve_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void Resolve_NonLocalWhenDoNotHaveThisTypeInParentButHaveInAParentOfParent_ShouldGetFromParentOfParent()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var grandParentContainer = Builder.Build();
        using var parentContainer = grandParentContainer.CreateChildContainer().Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void Resolve_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<NullReferenceException>(() => container.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void Resolve_AnyWhenTypeExistsInCurrentContainerAndInParent_GetFromCurrent()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        // Act
        // Assert
        Assert.That(childContainer.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void Resolve_AnyWhenTypeExistsInParentButNotInCurrentContainer_GetFromParent()
    {
        // Arrange
        Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = Builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        // Act
        // Assert
        Assert.That(childContainer.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }
}