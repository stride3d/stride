using Stride.Engine;

namespace SpaceEscape
{
    class SpaceEscapeApp
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
