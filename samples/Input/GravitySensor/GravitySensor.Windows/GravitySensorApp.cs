using Stride.Engine;

namespace GravitySensor
{
    class GravitySensorApp
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
