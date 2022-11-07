namespace DIFixture.Test_classes
{
    internal sealed class ClassWithManyConstructors
    {
        IErrorLogger errorLogger;
        public ClassWithManyConstructors() { }
        public ClassWithManyConstructors(IErrorLogger logger) 
        { 
            errorLogger = logger;
        }
    }
}
