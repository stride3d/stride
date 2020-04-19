using Stride.Engine;

namespace ParticlesSample
{
    class ParticlesSampleApp
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
