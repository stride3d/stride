using Stride.Engine;

namespace JumpyJet
{
    class JumpyJetApp
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
