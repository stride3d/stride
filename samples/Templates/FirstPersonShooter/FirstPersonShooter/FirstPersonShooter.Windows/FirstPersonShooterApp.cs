using Stride.Engine;

namespace FirstPersonShooter
{
    class FirstPersonShooterApp
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
