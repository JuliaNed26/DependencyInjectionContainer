using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIFixture.TestClasses
{
    public class ProgrammersProblem : IProblem
    {
        IProgrammer programmer;

        public ProgrammersProblem(IProgrammer _programmer)
        {
            programmer = _programmer;
        }

        public string GetProblemInfo()
        {
            return $"{programmer.GetName()}, ti ochen stupid, forgot to place }}.";
        }
    }
}
