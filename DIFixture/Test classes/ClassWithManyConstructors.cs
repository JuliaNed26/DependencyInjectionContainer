namespace DIFixture.Test_classes;

internal sealed class ClassWithManyConstructors
{
    public ClassWithManyConstructors() => ConstructorUsed = "Parameterless";
    public ClassWithManyConstructors(IErrorLogger errorLogger) 
    {
        ConstructorUsed = "With IErrorLogger";
    }
    public ClassWithManyConstructors(IUserDirectory userDirectory)
    {
        ConstructorUsed = "With IUserDirectory";
    }

    public string ConstructorUsed { get; init; }
}
