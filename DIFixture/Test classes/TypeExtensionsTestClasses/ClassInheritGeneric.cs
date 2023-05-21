using System.Collections;

namespace DIFixture.Test_classes.TypeExtensionsTestClasses;
internal class ClassInheritGeneric : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
