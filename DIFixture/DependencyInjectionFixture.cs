using DependencyInjectionContainer;
using DIFixture.Test_classes;
using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer.Enums;

namespace DIFixture
{
    public class DependencyInjectionFixture
    {
        DIContainerBuilder builder;

        [SetUp]
        public void Setup()
        {
            builder = new DIContainerBuilder();
        }

        [Test]
        public void DIContainerBuilderRegister_TypeWithManyCtors_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>( () => builder.Register<ClassWithManyConstructors>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<ClassWithManyConstructors>(ServiceLifetime.Transient));
        }

        [Test]
        public void DIContainerBuilderRegister_TwoEqualImplementationTypes_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<FileLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
            var obj = new FileLogger();
            Assert.Throws<ArgumentException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
        }

        [Test]
        public void DIContainerBuilderRegister_ByInterfaceOnly_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger>(ServiceLifetime.Singleton));
        }

        [Test]
        public void DIContainerBuilderRegisterWithImplementation_ShouldResolveByImplementationType()
        {
            IErrorLogger logger = new FileLogger();
            builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.That((IErrorLogger)container.Resolve<FileLogger>(), Is.EqualTo(logger));
        }

        [Test]
        public void DIContainerBuilderRegisterWithImplementation_ResolveByInterfaceType_ShouldThrowServiceNotFoundException()
        {
            IErrorLogger logger = new FileLogger();
            builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
        }

        [Test]
        public void DIContainerBuilderRegister_RegisterAfterBuild_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            var container = builder.Build(); 
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<ConsoleLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Transient));
            var obj = new ConsoleLogger();
            Assert.Throws<ArgumentException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
        }

        [Test]
        public void DIContainerBuilderBuild_TheSecondBuild_ReturnsTheSameContainer()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            var container1 = builder.Build();
            var container2 = builder.Build();
            Assert.That(container1, Is.EqualTo(container2));
        }

        [Test]
        public void DIContainerBuilderRegisterByAssembly_ShouldGetOnlyTypesWithRegisterAttributeWhenResolve()
        {
            builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
            var container = builder.Build();
            Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLogger)));
            Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectory)));
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
        }

        [Test]
        public void DIContainerBuilderRegister_RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.That(obj1, Is.EqualTo(obj2));
        }

        [Test]
        public void DIContainerBuilderRegister_RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            var container = builder.Build();
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.That(obj1, Is.Not.EqualTo(obj2));
        }

        [Test]
        public void DIContainerBuilderRegister_RegisterImplementationTypeInAChildWhenItExistsInParent_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var childBuilder = container.CreateChildContainer();
            Assert.Throws<ArgumentException>(() => childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
        }

        [Test]
        public void DIContainerBuilderResolve_ComplexGraph_ShouldReturnImplementation()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IUserDirectory, PublicDirectory>(ServiceLifetime.Transient);
            builder.Register<IUserDirectory,HiddenDirectory>(ServiceLifetime.Transient);
            builder.Register<IUserFile,SystemFile>(ServiceLifetime.Transient);
            builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.That(container.Resolve<FileSystem>().GetType(), Is.EqualTo(typeof(FileSystem)));
        }

        [Test]
        public void DIContainerResolve_NotAllTypesWasRegistered_ShouldThrowArgumentException()
        {
            builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<FileSystem>());
        }

        [Test]
        public void DIContainerResolve_ResolveIEnumerable_ShouldReturnEnumerableOfResolvedObjectsThatImplementsType()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolved = container.Resolve<IEnumerable<IErrorLogger>>();
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }

        [Test]
        public void DIContainerResolve_ResolveIEnumerableByImplementationType_ShouldReturnEnumerableWithOneObject()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolved = container.Resolve<IEnumerable<ConsoleLogger>>();
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(0));
        }

        [Test]
        public void DIContainerResolve_ResolveClassImplementsIEnumerableWhichNotRegistered_ShouldThrowAnException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<List<IErrorLogger>>());
        }

        [Test]
        public void DIContainerResolve_HasManyServicesImplementsCtorsParameterInterface_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            builder.Register<IUserDirectory, PublicDirectory>(ServiceLifetime.Transient);
            builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<FileSystem>());
        }

        [Test]
        public void DIContainerResolve_TypeRegisteredByImplementationTypeResolveByInterfaceType_ShouldThrowServiceNotFoundException()
        {
            builder.Register<FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
        }

        [Test]
        public void DIContainerResolve_ByInterfaceWhenTwoTypesImplementsIt_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<IErrorLogger>());
        }

        [Test]
        public void DIContainerResolveMany_ByInterfaceWhenTwoTypesInplementsIt_ShouldGetEnumerableOfServices()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolved = container.ResolveMany<IErrorLogger>();
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }

        [Test]
        public void DIContainerResolve_Local_ShouldGetTypeOnlyFromACurrentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Local).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        [Test]
        public void DIContainerResolveMany_Local_ShouldGetTypesOnlyFromACurrentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.Local);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        [Test]
        public void DIContainerResolve_LocalWhenNotRegisteredInСurrentContainerButRegisteredInParent_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            var childContainer = childBuilder.Build();
            Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveSource.Local));
        }

        [Test]
        public void DIContainerResolveMany_LocalWhenNotRegisteredInСurrentContainerButRegisteredInParent_ShouldReturnEmptyIEnumerable()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveSource.Local).Count(), Is.EqualTo(0));
        }

        [Test]
        public void DIContainerResolve_NonLocal_ShouldGetTypeOnlyFromAParentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void DIContainerResolveMany_NonLocal_ShouldGetTypesOnlyFromAParentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.NonLocal);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void DIContainerResolve_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldThrowArgumentException()
        {
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal));
        }

        [Test]
        public void DIContainerResolveMany_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldReturnEmptyIEnumerable()
        {
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveSource.NonLocal).Count(), Is.EqualTo(0));
        }

        [Test]
        public void DIContainerResolve_NonLocalWhenDontHaveThisTypeInParentButHaveInAParentOfParent_ShouldGetFromParentOfParent()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var grandParentContainer = builder.Build();
            var parentBuilder = grandParentContainer.CreateChildContainer();
            var parentContainer = parentBuilder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void DIContainerResolve_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<NullReferenceException>(() => container.Resolve<IErrorLogger>(ResolveSource.NonLocal));
        }

        [Test]
        public void DIContainerResolveMany_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<NullReferenceException>(() => container.ResolveMany<IErrorLogger>(ResolveSource.NonLocal));
        }

        [Test]
        public void DIContainerResolve_AnyWhenTypeExistsInCurrentContainerAndInParent_GetFromCurrent()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Any).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        [Test]
        public void DIContainerResolve_AnyWhenTypeExistsInParentButNotInCurrentContainer_GetFromParent()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Any).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void DIContainerResolveMany_AnyWhenTypeExistsInCurrentContainerAndInParent_GetFromBoth()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.Any);
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }

        [Test]
        public void DIContainerResolve_TypeWithValueTypeParameter_ShouldThrowAnException()
        {
            builder.Register<TypeWithIntParameter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<TypeWithIntParameter>());
        }
    }
}