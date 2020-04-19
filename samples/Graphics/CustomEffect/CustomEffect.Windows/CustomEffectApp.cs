using Stride.Engine;

namespace CustomEffect
{
    class CustomEffectApp
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
