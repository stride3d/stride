using Xenko.Engine;

namespace CSharpBeginner
{
    class CSharpBeginnerApp
    {
        static void Main(string[] args)
        {
            using (var game = new Xenko.Engine.Game())
            {
                game.Run();
            }
        }
    }
}
