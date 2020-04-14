using Stride.Engine;

namespace SpriteStudioDemo
{
    class SpriteStudioDemoApp
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
