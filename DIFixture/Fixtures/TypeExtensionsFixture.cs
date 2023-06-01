using DependencyInjectionContainer;

using DIFixture.Test_classes.TypeExtensionsTestClasses;

namespace DIFixture.Fixtures;

internal class TypeExtensionsFixture
{
    [Test]
    public void IsEnumerable_ForIEnumerable_ShouldReturnTrue()
    {
        // Arrange
        // Act
        // Assert
        Assert.IsTrue(typeof(IEnumerable<int>).IsEnumerable());
    }

    [Test]
    public void IsEnumerable_ForClassImplementIEnumerable_ShouldReturnFalse()
    {
        // Arrange
        List<int> enumerable = new();
        // Act
        // Assert
        Assert.IsFalse(enumerable.GetType().IsEnumerable());
    }

    [Test]
    public void IsEnumerable_ForNotEnumerable_ShouldReturnFalse()
    {
        // Arrange
        string obj = String.Empty;
        // Act
        // Assert
        Assert.IsFalse(obj.GetType().IsEnumerable());
    }

    [Test]
    public void GetGenericNameWithoutGenericType_ForGenericType_ReturnsNameWithoutDefinition()
    {
        // Arrange
        List<int> list = new();
        string expected = list.GetType().FullName!.Split('[')[0];
        // Act
        // Assert
        Assert.That(list.GetType().GetGenericNameWithoutGenericType(), Is.EqualTo(expected));
    }

    [Test]
    public void GetGenericNameWithoutGenericType_ForNonGenericType_ReturnsNull()
    {
        // Arrange
        string obj = String.Empty;
        // Act
        // Assert
        Assert.IsNull(obj.GetType().GetGenericNameWithoutGenericType());
    }

    [Test]
    public void IsAssignableToGenericTypeDefinition_GenericTypeWhichInheritsGenericType_ShouldReturnTrue()
    {
        // Arrange
        List<int> list = new();
        // Act
        // Assert
        Assert.IsTrue(list.GetType().IsAssignableToGenericTypeDefinition(typeof(IEnumerable<>)));
    }

    [Test]
    public void IsAssignableToGenericTypeDefinition_GenericTypeWhichNotInheritsGenericType_ShouldReturnFalse()
    {
        // Arrange
        List<int> list = new();
        // Act
        // Assert
        Assert.IsFalse(list.GetType().IsAssignableToGenericTypeDefinition(typeof(IDictionary<,>)));
    }

    [Test]
    public void IsAssignableToGenericTypeDefinition_TypeToCheckIsNotGenericTypeDefinition_ShouldThrowArgumentException()
    {
        // Arrange
        List<int> list = new();
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => list.GetType().IsAssignableToGenericTypeDefinition(typeof(IDisposable)));
    }

    [Test]
    public void IsAssignableToGenericTypeDefinition_TypeInheritsTypeWhichInheritsGenericType_ReturnsTrue()
    {
        // Arrange
        ClassInheritClassWhichImplementGeneric obj = new();
        // Act
        // Assert
        Assert.IsTrue(obj.GetType().IsAssignableToGenericTypeDefinition(typeof(IEnumerable<>)));
    }
}
