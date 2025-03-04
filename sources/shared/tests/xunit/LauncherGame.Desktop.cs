using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.stride
{
    class Program
    {
        public static void Main(string[] args) => StrideXunitRunner.Main(args, interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode);
    }
}
