using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.Test_classes
{
    internal sealed class FileSystem
    {
        private IEnumerable<IUserFile> userFiles;
        private IEnumerable<IUserDirectory> userDirectories;
        IErrorLogger errorNotificator;
        public FileSystem(IEnumerable<IUserFile> files, IEnumerable<IUserDirectory> directories, IErrorLogger _errorNotificator)
        {
            userFiles = files;
            userDirectories = directories;
            errorNotificator = _errorNotificator;
        }
    }
}
