namespace DIFixture.Test_classes
{
    internal sealed class HiddenDirectory : IUserDirectory
    {
        public string GetInfo()
        {
            return "This is a hidden directory";
        }
    }
}
