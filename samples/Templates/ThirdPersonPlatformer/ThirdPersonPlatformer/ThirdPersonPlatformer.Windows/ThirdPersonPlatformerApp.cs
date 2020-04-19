using Stride.Engine;

namespace ThirdPersonPlatformer
{
    class ThirdPersonPlatformerApp
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
