using DependencyInjectionContainer.Enums;

using DIFixture.Test_classes;

namespace DIFixture.Fixtures;

internal class DiContainerBuilderFixture : BaseFixture
{
    [Test]
    public void Build_TheSecondBuild_ShouldThrowInvalidOperationException()
    {
        // Arrange
        Builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = Builder.Build();
        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() => Builder.Build());
    }
}