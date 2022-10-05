using DependencyInjectionContainer;
using System.ComponentModel;
using DIFixture.TestClasses;
using System.Reflection;
using System.ComponentModel.Composition;

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
        public void RegisterTwoServicesWithEqualImplementationType_ThrowsArgumentException()
        {
            builder.Register<FileMessageWriter>(ServiceLifetime.Singleton);
            Assert.Throws<ArgumentException>(() => builder.Register<FileMessageWriter>(ServiceLifetime.Transient));
            Assert.Throws<ArgumentException>(() => builder.Register<FileMessageWriter>(ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterTypeOnlyByInterface_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => builder.Register<IMessagePrinter>(ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterTypeWithMoreThanOneConstructor_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => builder.Register<TypeWithManyCtorsWithoutAttributes>(ServiceLifetime.Singleton));
        }

        [Test]
        public void ResolveSingletone_ShouldReturnOneImplementation()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var firstResolveResult = container.Resolve<IMessagePrinter>();
            var secondResolveResult = container.Resolve<IMessagePrinter>();
            Assert.That(firstResolveResult, Is.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveTransient_ShouldReturnDifferentImplementations()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Transient);
            var container = builder.Build();
            var firstResolveResult = container.Resolve<IMessagePrinter>();
            var secondResolveResult = container.Resolve<IMessagePrinter>();
            Assert.That(firstResolveResult, Is.Not.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveComplexGraph()
        {
            builder.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            builder.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Singleton);
            builder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            builder.Register<INotifier, NotifierWithLocalPrinter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var notifier = container.Resolve<INotifier>();
            Assert.That(notifier.GetNotifyMessage(), Is.EqualTo("Andrey, ti ochen stupid, forgot to place }."));
        }

        [Test]
        public void ResolveMany_ShouldAllBeResolved()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            builder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolvedObjects = container.ResolveMany<IMessagePrinter>();
            Assert.That(resolvedObjects.Where(item => item.GetType() == typeof(FileMessageWriter)
                        || item.GetType() == typeof(ConsoleMessageWriter)).Count, Is.EqualTo(2));
            Assert.That(resolvedObjects.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ResolveServiceByInterface_MoreThanOneImplementationTypeRegistered_ShouldThrowException()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            builder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<IMessagePrinter>());
        }

        [Test]
        public void ResolveServiceWithNotAllInjectionsRegistered_ThrowNullReferenceException()
        {
            builder.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<NullReferenceException>(() => container.Resolve<IProblem>());
            //should IProgrammer interface implementation be registered
        }

        [Test]
        public void ResolveManyForNotAbstractType_ShouldResolveOneObject()
        {
            builder.Register<AndreyDeveloper>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolvedObjects = container.ResolveMany<AndreyDeveloper>();
            Assert.That(resolvedObjects.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RegisterTypeWithAttribute_ShouldBeRegisteredAuthomatically()
        {
            builder.RegisterAssemblyByAttributes(Assembly.GetExecutingAssembly());
            var container = builder.Build();
            var resolvedService = container.Resolve<ITypeWithAttribute>();
            Assert.That(resolvedService.GetType(), Is.EqualTo(typeof(TypeWithRegisterAttribute)));
        }

        [Test]
        public void ResolveTypeWhichRegisteredInChildAndInParentContainerLocal_GetResolvedFromAChild()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            Assert.That(childContainer.Resolve<IMessagePrinter>(ImportSource.Local).GetType(), Is.EqualTo(typeof(ConsoleMessageWriter)));
        }

        [Test]
        public void ResolveTypeWhichRegisteredInChildAndInParentContainerNonLocal_GetResolvedFromAParent()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            Assert.That(childContainer.Resolve<IMessagePrinter>(ImportSource.NonLocal).GetType(), Is.EqualTo(typeof(FileMessageWriter)));
        }

        [Test]
        public void ResolveTypeWhichRegisteredInChildAndInParentContainerAny_GetResolvedFromAChild()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            Assert.That(childContainer.Resolve<IMessagePrinter>(ImportSource.Any).GetType(), Is.EqualTo(typeof(ConsoleMessageWriter)));
        }
        [Test]
        public void ResolveTypeInContainerWithoutParentNonLocal_ShouldThrowException()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var container = builder.Build();
            Assert.Throws<ArgumentException>(() => container.Resolve<IMessagePrinter>(ImportSource.NonLocal).GetType());
        }

        [Test]
        public void ResolveTypeWhichRegisteredInParentButNotInChildContainer_FromChild_GetResolvedFromParent()
        {
            builder.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Transient); 
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            var progProblem = childContainer.Resolve<IProblem>();
            Assert.That(progProblem.GetProblemInfo, Is.EqualTo("Andrey, ti ochen stupid, forgot to place }."));
        }

        [Test]
        public void ResolveManyImportSourceTypeAny_ShouldReturnFromAChildAndParent()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton); 
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            Assert.That(childContainer.ResolveMany<IMessagePrinter>(ImportSource.Any).Count(), Is.EqualTo(2));
        }

        [Test]
        public void ResolveManyImportSourceTypeLocal_ShouldReturnFromAChildOnly()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            var resolved = childContainer.ResolveMany<IMessagePrinter>(ImportSource.Local);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleMessageWriter)));
        }

        [Test]
        public void ResolveManyImportSourceTypeNonLocal_ShouldReturnFromAChildOnly()
        {
            builder.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateAChildContainer();
            childContainerBuilder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = childContainerBuilder.Build();
            var resolved = childContainer.ResolveMany<IMessagePrinter>(ImportSource.NonLocal);
            Assert.That(resolved.Count(), Is.EqualTo(1));
            Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileMessageWriter)));
        }

        [Test]
        public void ResolveWithImportManyAttribute_ImportSourceNotLocal_GetNotLocal()
        {
            builder.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            builder.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Singleton);
            builder.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var parentContainer = builder.Build();
            var childBuilder = parentContainer.CreateAChildContainer();
            childBuilder.Register<INotifier, NotifierWithNonLocalPrinter>(ServiceLifetime.Singleton);
            var childContainer = childBuilder.Build();
            var notifier = childContainer.Resolve<INotifier>();
            Assert.That((notifier as NotifierWithNonLocalPrinter).Printer is ConsoleMessageWriter);
        }

        [Test]
        public void ResolveWithImportCtor_ShouldUseImportCtor()
        {
            builder.Register<TypeWithManyCtorsOneImportCtor>(ServiceLifetime.Singleton);
            var container = builder.Build();
            var resolved = container.Resolve<TypeWithManyCtorsOneImportCtor>();
            Assert.That(resolved.CtorMessage, Is.EqualTo("import constructor"));
        }
    }
}