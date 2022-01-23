using Stride.Engine;

namespace CSharpIntermediate
{
    class CSharpIntermediateApp
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
