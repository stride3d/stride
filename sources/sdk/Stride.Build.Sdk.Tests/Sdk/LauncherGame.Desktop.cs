
using Stride.Graphics.Regression;

namespace xunit.runner.stride
{
    class Program
    {
        public static void Main(string[] args) => StrideXunitRunner.Main(args, interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode, forceSaveImage => GameTestBase.ForceSaveImageOnSuccess = forceSaveImage);
    }
}
