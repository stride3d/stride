using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.xenko
{
    class Program
    {
        public static void Main(string[] args) => XenkoXunitRunner.Main(args, interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode);
    }
}
