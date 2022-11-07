namespace DIFixture.Test_classes
{
    internal sealed class FileSystem
    {
        private IEnumerable<IUserFile> userFiles;
        private IEnumerable<IUserDirectory> userDirectories;
        IErrorLogger errorNotificator;
        internal FileSystem(IEnumerable<IUserFile> files, IEnumerable<IUserDirectory> directories, IErrorLogger notificator)
        {
            userFiles = files;
            userDirectories = directories;
            errorNotificator = notificator;
        }
    }
}
