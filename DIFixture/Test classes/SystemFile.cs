namespace DIFixture.Test_classes
{
    internal sealed class SystemFile : IUserFile
    {
        public string GetInfo()
        {
            return "This is a system file";
        }

        public bool TryOpen() => false;
    }
}
