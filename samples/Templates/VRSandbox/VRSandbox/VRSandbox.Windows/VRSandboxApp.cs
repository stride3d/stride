using Stride.Engine;

namespace VRSandbox
{
    class VRSandboxApp
    {
        static void Main(string[] args)
        {
            using (var game = new VRGame())
            {
                game.Run();
            }
        }
    }
}
