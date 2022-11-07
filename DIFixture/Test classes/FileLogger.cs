namespace DIFixture.Test_classes
{
    internal sealed class FileLogger : IErrorLogger
    {
        public string Log(string message)
        {
            return $"Logged into a file with message: {message}";
        }
    }
}
