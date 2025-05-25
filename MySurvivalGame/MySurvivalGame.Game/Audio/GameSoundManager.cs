using Stride.Core.Mathematics;
using Stride.Engine; // Required for Log
using Stride.Core.Diagnostics; // Required for ILogger and GlobalLogger

namespace MySurvivalGame.Game.Audio
{
    public static class GameSoundManager
    {
        private static ILogger Log = GlobalLogger.GetLogger(nameof(GameSoundManager));

        public static void PlaySound(string soundName, Vector3 position)
        {
            // For now, just log. Later, this will interact with Stride's audio engine.
            Log.Info($"Playing sound: '{soundName}' at position {position}");
        }

        // Overload for sounds not tied to a specific world position (e.g., UI clicks)
        public static void PlaySound(string soundName)
        {
            Log.Info($"Playing sound: '{soundName}' (non-positional).");
        }
    }
}
