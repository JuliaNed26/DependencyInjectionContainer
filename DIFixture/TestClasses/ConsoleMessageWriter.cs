using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    public class ConsoleMessageWriter : IMessagePrinter
    {
        public void Print(string message)
        {
            Console.WriteLine(message);
        }
    }
}
