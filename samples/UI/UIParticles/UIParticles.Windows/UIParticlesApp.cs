using Stride.Engine;

namespace UIParticles
{
    class UIParticlesApp
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
