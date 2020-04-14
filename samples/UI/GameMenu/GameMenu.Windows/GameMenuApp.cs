using Stride.Engine;

namespace GameMenu
{
    class GameMenuApp
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
