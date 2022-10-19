using DependencyInjectionContainer;
using System.ComponentModel;
using System.Reflection;
using System.ComponentModel.Composition;
using DIFixture.Test_classes;
using DependencyInjectionContainer.Exceptions;

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

        //register type with many constructors fails
        [Test]
        public void RegisterTypeWithManyCtors_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            Assert.Throws<ArgumentException>( () => builder.Register<ClassWithManyConstructors>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<ClassWithManyConstructors>(ServiceLifetime.Transient));
        }

        //register two equal types
        [Test]
        public void RegisterTwoEqualTypes_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<FileLogger>(ServiceLifetime.Singleton));
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
            var obj = new FileLogger();
            Assert.Throws<ArgumentException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterByInterfaceOnly_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => builder.Register<IErrorLogger>(ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterWithImplementation_RegisterObjWithInterfaceTypeButImplementedByClass_ShouldBuild()
        {
            IErrorLogger logger = new FileLogger();
            builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
            Assert.NotNull(builder.Build());// don't know how to check that this line doesn't throw exception
        }

        [Test]
        public void RegisterAfterBuild_ShouldThrowArgumentException()
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
        public void TheSecondBuildReturnsTheSameContainer()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            var container1 = builder.Build();
            var container2 = builder.Build();
            Assert.That(container1, Is.EqualTo(container2));
        }

        //register by assembly
        [Test]
        public void RegisterByAssembly_ShouldGetOnlyTypesWithAttributesWhenResolve()
        {
            builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
            var container = builder.Build();
            Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLogger)));
            Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectory)));
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
        }

        //register singletone
        [Test]
        public void RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.That(obj1, Is.EqualTo(obj2));
        }

        //register transient
        [Test]
        public void RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
            var container = builder.Build();
            var obj1 = container.Resolve<IErrorLogger>();
            var obj2 = container.Resolve<IErrorLogger>();
            Assert.That(obj1, Is.Not.EqualTo(obj2));
        }

        //register two different types with the same interface
        [Test]
        public void RegisterTwoDifferentTypesWithTheSameInterface_ShouldBuild()
        {
            builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
            builder.Register<IUserFile, SystemFile>(ServiceLifetime.Transient);
            Assert.NotNull(builder.Build());// don't know how to check that this line doesn't throw exception
        }

        [Test]
        public void RegisterImplementationTypeInAChildWhenItExistsInParent_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var childBuilder = container.CreateAChildContainer();
            Assert.Throws<ArgumentException>(() => childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
        }

        //simple resolve
        [Test]
        public void ResolveComplexGraph_ShouldPass()
        {
            builder.RegisterAssemblyByAttributes(typeof(FileSystem).Assembly);
            builder.Register<IUserDirectory,HiddenDirectory>(ServiceLifetime.Transient);
            builder.Register<IUserFile,SystemFile>(ServiceLifetime.Transient);
            builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.That(container.Resolve<FileSystem>().GetType(), Is.EqualTo(typeof(FileSystem)));
        }

        [Test]
        public void ResolveWhenNotAllTypesRegistered_ShouldThrowArgumentException()
        {
            builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<FileSystem>());
        }

        [Test]
        public void ResolveIEnumerable_ShouldBeEqualToResolveMany()
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
        public void ResolveNotRegisteredList_ShouldThrowAnException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<List<IErrorLogger>>());
        }

        [Test]
        public void ResolveTypeInWhichCtorParameterTypeRegisteredManyTimes_ShouldThrowArgumentException()
        {
            builder.RegisterAssemblyByAttributes(typeof(FileSystem).Assembly);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
            builder.Register<FileSystem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<FileSystem>());
        }

        [Test]
        public void TypeRegisteredByImplementationTypeResolveByInterfaceType_ShouldThrowKeyNotFoundException()
        {
            builder.Register<FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
        }

        //resolve when two services registered by this interface failed
        [Test]
        public void ResolveByInterfaceWhenTwoTypesInplementsIt_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<IErrorLogger>());
        }

        //resolve many  when two services registered by this interface
        [Test]
        public void ResolveManyByInterfaceWhenTwoTypesInplementsIt_ShouldGetAllTypes()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolved = container.ResolveMany<IErrorLogger>();
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }

        //resolve local
        [Test]
        public void ResolveLocal_ShouldGetTypeFromAChildContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Local).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        [Test]
        public void ResolveManyLocal_ShouldGetTypesOnlyFromAChildContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.Local);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        //resolve local when don't have this type in a child fails
        [Test]
        public void ResoveLocalWhenNotRegisteredInChildButRegisteredInParent_ShouldThrowArgumentException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            var childContainer = childBuilder.Build();
            Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveSource.Local));
        }

        [Test]
        public void ResoveManyLocalWhenNotRegisteredInChildButRegisteredInParent_ShouldReturnEmptyIEnumerable()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveSource.Local).Count(), Is.EqualTo(0));
        }

        //resolve non local
        [Test]
        public void ResolveNonLocal_ShouldGetTypeFromAParentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void ResolveManyNonLocal_ShouldGetTypesOnlyFromAParentContainer()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.NonLocal);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        //resolve non local when don't have this type in a parent fails
        [Test]
        public void ResoveNonLocalWhenNotRegisteredInParentButRegisteredInChild_ShouldThrowArgumentException()
        {
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal));
        }

        [Test]
        public void ResoveManyNonLocalWhenNotRegisteredInParentButRegisteredInChild_ShouldReturnEmptyIEnumerable()
        {
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveSource.NonLocal).Count(), Is.EqualTo(0));
        }

        //resolve non local when don't have this type in a parent, but have in a parent of parent
        [Test]
        public void ResolveNonLocalWhenDontHaveThisTypeInParentButHaveInParentOfParent_ShouldGetFromParentOfParent()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            var childContainer = childBuilder.Build();
            var childChildBuilder = childContainer.CreateAChildContainer();
            childChildBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childChildContainer = childChildBuilder.Build();
            Assert.That(childChildContainer.Resolve<IErrorLogger>(ResolveSource.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void ResolveNonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<NullReferenceException>(() => container.Resolve<IErrorLogger>(ResolveSource.NonLocal));
        }

        [Test]
        public void ResolveManyNonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<NullReferenceException>(() => container.ResolveMany<IErrorLogger>(ResolveSource.NonLocal));
        }
        //resolve any 
        [Test]
        public void ResolveAnyWhenTypeExistsInChildAndInParent_GetFromChild()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Any).GetType(), Is.EqualTo(typeof(FileLogger)));
        }

        //resolve any when don't have in a child, get from a parent
        [Test]
        public void ResolveAnyWhenTypeExistsInParentButNotInChild_GetFromParent()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            var childContainer = childBuilder.Build();
            Assert.That(childContainer.Resolve<IErrorLogger>(ResolveSource.Any).GetType(), Is.EqualTo(typeof(ConsoleLogger)));
        }

        [Test]
        public void ResolveManyAnyWhenTypeExistsInChildAndInParent_GetFromBoth()
        {
            builder.Register<IErrorLogger, ConsoleLogger>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveSource.Any);
            Assert.That(resolved.Count(), Is.EqualTo(2));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(ConsoleLogger)).Count(), Is.EqualTo(1));
            Assert.That(resolved.Where(logger => logger.GetType() == typeof(FileLogger)).Count(), Is.EqualTo(1));
        }

        [Test]
        public void RegisterTypeWithValueParameter_ShouldThrowAnException()
        {
            builder.Register<TypeWithIntParameter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<TypeWithIntParameter>());
        }
    }
}