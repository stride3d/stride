using Stride.Engine;

namespace TouchInputs
{
    class TouchInputsApp
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
