using Stride.Engine;

namespace BepuPhysicIntegrationTest
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
