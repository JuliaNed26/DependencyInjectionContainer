namespace DIFixture.Test_classes
{
    internal class GenericClass<T>
    {
        public T givenParam;
        public GenericClass(T param) => givenParam = param;
    }
}
