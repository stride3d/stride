
using Stride.Graphics.Regression;
using xunit.runner.stride.ViewModels;

namespace xunit.runner.stride
{
    class Program
    {
        public static void Main(string[] args) => StrideXunitRunner.Main(args,
            interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode,
            forceSaveImage => GameTestBase.ForceSaveImageOnSuccess = forceSaveImage,
            renderDocMode => GameTestBase.RenderDocMode = renderDocMode,
            subscribe => ImageTester.ImageComparisonCompleted += (s, e) =>
                subscribe(new ImageCompareResult(e.CurrentPath, e.ReferencePath, e.Passed, e.Stats.ToString())));
    }
}
