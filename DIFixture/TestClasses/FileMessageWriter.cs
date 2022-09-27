using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    public class FileMessageWriter : IMessagePrinter
    {
        string filePath = "C:\\Users\\nedav\\Desktop\\in.txt";

        public void Print(string message)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(message);
            }
        }
    }
}
