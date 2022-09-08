using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    public class Notifier
    {
        IProblem problem;
        IMessagePrinter printer;
        public Notifier(IProblem _problem, IMessagePrinter _printer)
        {
            problem = _problem;
            printer = _printer;
        }

        public string GetNotifyMessage()
        {
            return problem.GetProblemInfo();
        }

        public void Notify()
        {
            printer.Print(problem.GetProblemInfo());
        }
    }
}
