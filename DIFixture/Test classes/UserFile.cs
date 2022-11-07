namespace DIFixture.Test_classes
{
    internal sealed class UserFile : IUserFile
    {
        public string GetInfo()
        {
            return "This is a user file";
        }

        public bool TryOpen() => true;
    }
}
