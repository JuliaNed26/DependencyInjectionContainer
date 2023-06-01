using DependencyInjectionContainer;

namespace DIFixture.Fixtures;

internal class BaseFixture
{
    protected DiContainerBuilder Builder { get; set; }

    [SetUp]
    public void Setup()
    {
        Builder = new DiContainerBuilder();
    }
}