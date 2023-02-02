namespace DIFixture;
using DependencyInjectionContainer;
using DIFixture.Test_classes;
using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer.Enums;

public class DependencyInjectionFixture
{
    DIContainerBuilder builder;

    [SetUp]
    public void Setup()
    {
        builder = new DIContainerBuilder();
    }

    [Test]
    public void DIContainerBuilderRegister_TwoEqualImplementationTypesInContainer_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<FileLogger>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
        var obj = new FileLogger();
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
    }

    [Test]
    public void DIContainerBuilderRegister_ByInterfaceOnly_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger>(ServiceLifetime.Singleton));
    }

    [Test]
    public void DIContainerBuilderRegisterWithImplementation_ShouldResolveByImplementationType()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.That((IErrorLogger)container.Resolve<FileLogger>(), Is.EqualTo(logger));
        }
    }

    [Test]
    public void DIContainerBuilderRegisterWithImplementation_ResolveByInterfaceType_ShouldThrowServiceNotFoundException()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterAfterBuild_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using (var container = builder.Build())
        {
            Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
            Assert.Throws<RegistrationServiceException>(() => builder.Register<ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
            Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient));
            var obj = new ConsoleLoggerWithAttribute();
            Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterTypeWithManyCtorsNotDefineWhichToUse_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>( () => builder.Register<ClassWithManyConstructors>(ServiceLifetime.Singleton));
    }

    [Test]
    public void DIContainerBuilderBuild_TheSecondBuild_ShouldThrowInvalidOperationException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using (var container = builder.Build())
        {
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }
    }

    [Test]
    public void DIContainerBuilderRegisterByAssembly_ShouldGetOnlyTypesWithRegisterAttributeWhenResolve()
    {
        builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
        using (var container = builder.Build())
        {
            Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
            Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectoryWithAttribute)));
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.IsTrue(ReferenceEquals(obj1, obj2));
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using (var container = builder.Build())
        {
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.IsFalse(ReferenceEquals(obj1, obj2));
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterImplementationTypeInAChildContainerWhenItExistsInParent_ShouldOverrideParentsRegistration()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            var childBuilder = container.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            using (var childContainer = childBuilder.Build())
            {
                Assert.IsFalse(ReferenceEquals(container.Resolve<IErrorLogger>(), childContainer.Resolve<IErrorLogger>()));
            }
        }
    }

    [Test] public void DIContainerResolve_ResolveServiceWithManyCtorsWhereImplementationFactoryWasDefined_ShouldResolveByFactory()
    {
        builder.Register<ClassWithManyConstructors>(ServiceLifetime.Transient, 
                                          container => new ClassWithManyConstructors());
        using (var container = builder.Build())
        {
            var resolved = container.Resolve<ClassWithManyConstructors>();
            Assert.That(resolved.CtorUsed, Is.EqualTo("Parameterless"));

            var child1Builder = container.CreateChildContainer();
            child1Builder.Register<ClassWithManyConstructors>(ServiceLifetime.Transient,
                container =>
                {
                    var logger = container.Resolve<IErrorLogger>();
                    return new ClassWithManyConstructors(logger);
                });
            child1Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient);
            using(var childContainer = child1Builder.Build())
            {
                var resolvedFromChild = childContainer.Resolve<ClassWithManyConstructors>();
                Assert.That(resolvedFromChild.CtorUsed, Is.EqualTo("With IErrorLogger"));
            }
            var child2Builder = container.CreateChildContainer();
            child2Builder.Register<ClassWithManyConstructors>(ServiceLifetime.Transient,
                container =>
                {
                    var directory = container.Resolve<IUserDirectory>();
                    return new ClassWithManyConstructors(directory);
                });
            child2Builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
            using (var childContainer = child2Builder.Build())
            {
                var resolvedFromChild = childContainer.Resolve<ClassWithManyConstructors>();
                Assert.That(resolvedFromChild.CtorUsed, Is.EqualTo("With IUserDirectory"));
            }
        }

    }

    [Test]
    public void DIContainerResolve_ServiceRegisteredByInterfaceResolveByImplementationType_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<ConsoleLoggerWithAttribute>());
        }
    }

    [Test]
    public void DIContainerResolve_ComplexGraph_ShouldReturnImplementation()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        builder.Register<IUserFile, SystemFile>(ServiceLifetime.Transient);
        builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.That(container.Resolve<FileSystem>().GetType(), Is.EqualTo(typeof(FileSystem)));
        }
    }

    [Test]
    public void DIContainerResolve_NotAllCtorParametersWasRegistered_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<FileSystem>());
        }
    }

    [Test]
    public void DIContainerResolve_ResolveIEnumerable_ShouldReturnEnumerableOfResolvedObjectsThatImplementsType()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            var resolved = container.Resolve<IEnumerable<IErrorLogger>>();
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLoggerWithAttribute)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }
    }

    [Test]
    public void DIContainerResolve_ResolveClassImplementsIEnumerableWhichNotRegistered_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<List<IErrorLogger>>());
        }
    }

    [Test]
    public void DIContainerResolve_HasManyServicesThatImplementCtorsParameterInterface_ShouldThrowResolveServiceException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ResolveServiceException>(() => container.Resolve<FileSystem>());
        }
    }

    [Test]
    public void DIContainerResolve_ByInterfaceWhenTwoTypesImplementsItInOneContainer_ShouldThrowResolveServiceException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ResolveServiceException>(() => container.Resolve<IErrorLogger>());
        }
    }

    [Test]
    public void DIContainerResolve_Local_ShouldGetTypeOnlyFromACurrentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local).GetType(), Is.EqualTo(typeof(FileLogger)));
            }
        }
    }

    [Test]
    public void DIContainerResolve_LocalWhenNotRegisteredInСurrentContainerButRegisteredInParent_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            using (var childContainer = parentContainer.CreateChildContainer().Build())
            {
                Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local));
            }
        }
    }

    [Test]
    public void DIContainerResolve_NonLocal_ShouldGetTypeOnlyFromAParentContainers()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
            }
        }
    }

    [Test]
    public void DIContainerResolve_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldThrowServiceNotFoundException()
    {
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
            }
        }
    }

    [Test]
    public void DIContainerResolve_NonLocalWhenDontHaveThisTypeInParentButHaveInAParentOfParent_ShouldGetFromParentOfParent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var grandParentContainer = builder.Build())
        {
            using (var parentContainer = grandParentContainer.CreateChildContainer().Build())
            {
                var childBuilder = parentContainer.CreateChildContainer();
                childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
                using (var childContainer = childBuilder.Build())
                {
                    Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
                }
            }
        }
    }

    [Test]
    public void DIContainerResolve_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<NullReferenceException>(() => container.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
        }
    }

    [Test]
    public void DIContainerResolve_AnyWhenTypeExistsInCurrentContainerAndInParent_GetFromCurrent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.Any).GetType(), Is.EqualTo(typeof(FileLogger)));
            }
        }
    }

    [Test]
    public void DIContainerResolve_AnyWhenTypeExistsInParentButNotInCurrentContainer_GetFromParent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            using (var childContainer = parentContainer.CreateChildContainer().Build())
            {
                Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.Any).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
            }
        }
    }

    [Test]
    public void DIContainerResolve_TypeWithValueTypeParameterInCtor_ShouldThrowArgumentException()
    {
        builder.Register<TypeWithIntParameter>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<ArgumentException>(() => container.Resolve<TypeWithIntParameter>());
        }
    }

    [Test]
    public void DIContainerResolve_TypeWithValueTypeParameterInCtorRegisteredByImplementation_ShouldBeResolved()
    {
        TypeWithIntParameter typeWithIntParameter = new TypeWithIntParameter(3);
        builder.RegisterWithImplementation(typeWithIntParameter, ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.IsTrue(ReferenceEquals(typeWithIntParameter, container.Resolve<TypeWithIntParameter>()));
        }
    }

    [Test]
    public void DIContainerResolveMany_ByInterfaceWhenTwoTypesImplementsIt_ShouldGetEnumerableOfServices()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            var resolved = container.ResolveMany<IErrorLogger>();
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLoggerWithAttribute)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }
    }

    [Test]
    public void DIContainerResolveMany_Local_ShouldGetTypesOnlyFromACurrentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local);
                Assert.That(resolved.Count(), Is.EqualTo(1));
                Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileLogger)));
            }
        }
    }

    [Test]
    public void DIContainerResolveMany_LocalWhenNotRegisteredInСurrentContainerButRegisteredInParent_ShouldReturnEmptyIEnumerable()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            using (var childContainer = parentContainer.CreateChildContainer().Build())
            {
                Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local).Count(), Is.EqualTo(0));
            }
        }
    }

    [Test]
    public void DIContainerResolveMany_NonLocal_ShouldGetTypesOnlyFromAParentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal);
                Assert.That(resolved.Count(), Is.EqualTo(1));
                Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
            }
        }
    }

    [Test]
    public void DIContainerResolveMany_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldReturnEmptyIEnumerable()
    {
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal).Count(), Is.EqualTo(0));
            }
        }
    }

    [Test]
    public void DIContainerResolveMany_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using (var container = builder.Build())
        {
            Assert.Throws<NullReferenceException>(() => container.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal));
        }
    }

    [Test]
    public void DIContainerResolveMany_AnyWhenTypeImplementsInterfaceExistsInCurrentContainerAndInParent_GetFromBoth()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Any);
                Assert.That(resolved.Count(), Is.EqualTo(2));
                Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLoggerWithAttribute)).Count(), Is.EqualTo(1));
                Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
            }
        }
    }

    [Test]
    public void DIContainerResolveMany_AnyWhenServicesOfSameTypeExistsInCurrentContainerAndInParent_GetFromChild()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using (var parentContainer = builder.Build())
        {
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
            using (var childContainer = childBuilder.Build())
            {
                var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Any);
                Assert.That(resolved.Count(), Is.EqualTo(1));
                Assert.IsTrue(ReferenceEquals(resolved.FirstOrDefault(), childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local)));
            }
        }
    }

    [Test]
    public void DIContainerBuilderRegister_RegisterTransientDisposable_ThrowsRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ChildDisposableClass>(ServiceLifetime.Transient));
    }

    [Test]
    public void DIContainerDispose_ChildContainerDisposed_ParentContainerShouldNotBeDisposed()
    {
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Singleton);
        {
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateChildContainer();
            childContainerBuilder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            childContainer.Dispose();
            Assert.Throws<InvalidOperationException>(() => childContainer.Dispose());
            parentContainer.Dispose();
        }
    }

    [Test]
    public void DIContainerDispose_ShouldBeDisposedStartingFromParentToChildClass()
    {
        builder.Register<DisposableRegistrator>(ServiceLifetime.Singleton);
        builder.Register<GrandParentDisposableClass>(ServiceLifetime.Singleton);
        builder.Register<ParentDisposableClass>(ServiceLifetime.Singleton);
        builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = builder.Build();
        var disposeSequence = container.Resolve<DisposableRegistrator>();
        container.Resolve<GrandParentDisposableClass>();
        container.Dispose();
        List<Type> expected = new List<Type>(){typeof(GrandParentDisposableClass), typeof(ParentDisposableClass), typeof(ChildDisposableClass)};
        CollectionAssert.AreEqual(expected, disposeSequence.GetDisposedClasses());
    }

    [Test]
    public void DIContainerDispose_DisposeSecondTime_ShouldThrowInvalidOperationException()
    {
        var container = builder.Build();
        container.Dispose();
        Assert.Throws<InvalidOperationException>(() => container.Dispose());
    }

    [Test]
    public void DIContainerDispose_ResolveAfterDispose_ShouldThrowInvalidOperationException()
    {
        builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = builder.Build();
        container.Dispose();
        Assert.Throws<InvalidOperationException>(() => container.Resolve<ChildDisposableClass>());
    }
}