namespace DependencyInjectionContainer.Enums
{
    [Flags]
    public enum Rules
    {
        None = 0,
        DisposeTransientWhenDisposeContainer = 1,
        GetConstructorWithMostRegisteredParameters = 2
    }

    [Flags]
    public enum SecondRegistrationAction
    {
        Throw,
        Ignore,
        Rewrite
    }

}
