using Stride.Engine;

namespace Stride.BepuPhysics.Demo.Windows
{
    class BepuPhysicIntegrationTestApp
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
