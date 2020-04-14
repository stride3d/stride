using Stride.Engine;

namespace SpriteFonts
{
    class SpriteFontsApp
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
