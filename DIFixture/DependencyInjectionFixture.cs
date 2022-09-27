using DependencyInjectionContainer;
using System.ComponentModel;
using DIFixture.TestClasses;
using System.Reflection;

namespace DIFixture
{
    public class DependencyInjectionFixture
    {
        DIContainer container;

        [SetUp]
        public void Setup()
        {
            container = new DIContainer();
        }

        [Test]
        public void RegisterTwoServicesWithEqualImplementationType_ThrowsArgumentException()
        {
            container.Register<FileMessageWriter>(ServiceLifetime.Singleton);
            Assert.Throws<ArgumentException>(() => container.Register<FileMessageWriter>(ServiceLifetime.Transient));
            Assert.Throws<ArgumentException>(() => container.Register<FileMessageWriter>(ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterTypeOnlyByInterface_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => container.Register<IMessagePrinter>(ServiceLifetime.Singleton));
        }

        [Test]
        public void RegisterTypeWithMoreThanOneConstructor_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => container.Register<TypeWithManyConstructors>(ServiceLifetime.Singleton));
        }

        [Test]
        public void ResolveSingletone_ShouldReturnOneImplementation()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var firstResolveResult = container.Resolve<IMessagePrinter>();
            var secondResolveResult = container.Resolve<IMessagePrinter>();
            Assert.That(firstResolveResult, Is.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveTransient_ShouldReturnDifferentImplementations()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Transient);
            var firstResolveResult = container.Resolve<IMessagePrinter>();
            var secondResolveResult = container.Resolve<IMessagePrinter>();
            Assert.That(firstResolveResult, Is.Not.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveComplexGraph()
        {
            container.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            container.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            container.Register<INotifier, Notifier>(ServiceLifetime.Singleton);
            var notifier = container.Resolve<INotifier>();
            Assert.That(notifier.GetNotifyMessage(), Is.EqualTo("Andrey, ti ochen stupid, forgot to place }."));
        }

        [Test]
        public void ResolveMany_ShouldAllBeResolved()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var resolvedObjects = container.ResolveMany<IMessagePrinter>();
            Assert.That(resolvedObjects.Where(item => item.GetType() == typeof(FileMessageWriter)
                        || item.GetType() == typeof(ConsoleMessageWriter)).Count, Is.EqualTo(2));
            Assert.That(resolvedObjects.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ResolveServiceByInterface_MoreThanOneImplementationTypeRegistered_ShouldThrowException()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            Assert.Throws<ArgumentException>(() => container.Resolve<IMessagePrinter>());
        }

        [Test]
        public void ResolveServiceWithNotAllInjectionsRegistered_ThrowNullReferenceException()
        {
            container.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            Assert.Throws<NullReferenceException>(() => container.Resolve<IProblem>());
            //should IProgrammer interface implementation be registered
        }

        [Test]
        public void ResolveManyForNotAbstractType_ShouldResolveOneObject()
        {
            container.Register<AndreyDeveloper>(ServiceLifetime.Singleton);
            var resolvedObjects = container.ResolveMany<AndreyDeveloper>();
            Assert.That(resolvedObjects.Count(), Is.EqualTo(1));
        }

        [Test]
        public void RegisterTypeWithAttribute_ShouldBeRegisteredAuthomatically()
        {
            container.RegisterAssemblyByAttributes(Assembly.GetExecutingAssembly());
            var resolvedService = container.Resolve<ITypeWithAttribute>();
            Assert.That(resolvedService.GetType(), Is.EqualTo(typeof(TypeWithAttribute)));
        }

        [Test]
        public void RegistrationAfterResolving_ThrowsAnException()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Resolve<IMessagePrinter>();
            Assert.Throws<ArgumentException>(() => container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton));
        }

        [Test]
        public void ResolveTypeWhichRegisteredInChildAndInParentContainer_FromChild_GetResolvedFromAChild()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = container.CreateAChildContainer();
            childContainer.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            Assert.That(childContainer.Resolve<IMessagePrinter>().GetType(), Is.EqualTo(typeof(ConsoleMessageWriter)));
        }

        [Test]
        public void ResolveTypeWhichRegisteredInParentButNotInChildContainer_FromChild_GetResolvedFromParent()
        {
            container.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Transient);
            var childContainer = container.CreateAChildContainer();
            childContainer.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            var progProblem = childContainer.Resolve<IProblem>();
            Assert.That(progProblem.GetProblemInfo, Is.EqualTo("Andrey, ti ochen stupid, forgot to place }."));
        }

        [Test]
        public void ResolveManyWithParentContainer_ShouldReturnFromAChildAndParent()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            var childContainer = container.CreateAChildContainer();
            childContainer.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            Assert.That(childContainer.ResolveMany<IMessagePrinter>(true).Count(), Is.EqualTo(2));
        }

    }
}