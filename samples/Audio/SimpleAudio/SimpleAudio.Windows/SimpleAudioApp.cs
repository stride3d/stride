using Stride.Engine;

namespace SimpleAudio
{
    class SimpleAudioApp
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
