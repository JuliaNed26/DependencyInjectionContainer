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
            container = new DIContainer(Assembly.GetExecutingAssembly());
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
        //refactoring
        [Test]
        public void RegisterAndResolveAfterResolving_ShouldResolveTwoServices()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register("message", ServiceLifetime.Singleton);
            var fileMesWriterResolveResult = container.Resolve<FileMessageWriter>();
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var consoleMesWriterResolveResult = container.Resolve<ConsoleMessageWriter>();
            Assert.That(fileMesWriterResolveResult.GetType, Is.EqualTo(typeof(FileMessageWriter)));
            Assert.That(consoleMesWriterResolveResult.GetType, Is.EqualTo(typeof(ConsoleMessageWriter)));
        }

        [Test]
        public void ResolveSingletone_ShouldReturnOneImplementation()
        {
            container.Register<IMessagePrinter,FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register("message", ServiceLifetime.Singleton);
            var firstResolveResult = container.Resolve<FileMessageWriter>();
            var secondResolveResult = container.Resolve<FileMessageWriter>();
            Assert.That(firstResolveResult, Is.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveTransient_ShouldReturnDifferentImplementations()
        {
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Transient);
            container.Register("message", ServiceLifetime.Singleton);
            var firstResolveResult = container.Resolve<FileMessageWriter>();
            var secondResolveResult = container.Resolve<FileMessageWriter>();
            Assert.That(firstResolveResult, Is.Not.EqualTo(secondResolveResult));
        }

        [Test]
        public void ResolveServiceByInterface_RegisteredTwoServicesWithThisInterface_ShouldGetTheFirst()
        {
            container.Register("message", ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            Type type = container.Resolve<IMessagePrinter>().GetType();
            Assert.That(type, Is.EqualTo(typeof(FileMessageWriter)));
        }

        [Test]
        public void ResolveComplexGraph()
        {
            container.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            container.Register<IProgrammer, AndreyDeveloper>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            container.Register<Notifier>(ServiceLifetime.Singleton);
            Notifier notifier = container.Resolve<Notifier>();
            Assert.That(notifier.GetNotifyMessage(), Is.EqualTo("Andrey, ti ochen stupid, forgot to place }."));
        }

        [Test]
        public void ResolveMany_ShouldAllBeResolved()
        {
            container.Register("message", ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);
            container.Register<IMessagePrinter, ConsoleMessageWriter>(ServiceLifetime.Singleton);
            var resolvedObjects = container.ResolveMany<IMessagePrinter>();
            Assert.That(resolvedObjects.Where(item => item.GetType() == typeof(FileMessageWriter) 
                        || item.GetType() == typeof(ConsoleMessageWriter)).Count,Is.EqualTo(2));
            Assert.That(resolvedObjects.Count(),Is.EqualTo(2));
        }

        [Test]
        public void ResolveServiceWithNotAllInjectionsRegistered_ThrowNullReferenceException()
        {
            container.Register<IProblem, ProgrammersProblem>(ServiceLifetime.Singleton);
            Assert.Throws<NullReferenceException>(() => container.Resolve<ProgrammersProblem>());
            container.Register<IMessagePrinter, FileMessageWriter>(ServiceLifetime.Singleton);//haven't registered string parameter path
            Assert.Throws<NullReferenceException>(() => container.Resolve<ConsoleMessageWriter>());
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
            var resolvedService = container.Resolve<TypeWithAttribute>();
            Assert.That(resolvedService.GetType(), Is.EqualTo(typeof(TypeWithAttribute)));
        }
    }
}