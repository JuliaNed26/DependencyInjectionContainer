using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    public class NotifierWithNonLocalPrinter : INotifier
    {
        IProblem problem;
        internal IMessagePrinter Printer { get; init; }
        public NotifierWithNonLocalPrinter(IProblem _problem, [ImportMany(Source = ImportSource.NonLocal)]IMessagePrinter _printer)
        {
            problem = _problem;
            Printer = _printer;
        }

        public string GetNotifyMessage()
        {
            return problem.GetProblemInfo();
        }

        public void Notify()
        {
            Printer.Print(problem.GetProblemInfo());
        }
    }
}
