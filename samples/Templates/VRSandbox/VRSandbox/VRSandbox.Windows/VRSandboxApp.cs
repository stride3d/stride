using Stride.Engine;

namespace VRSandbox
{
    class VRSandboxApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
