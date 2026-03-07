using System;
using System.Linq;

namespace Scenes.Arena
{
    /// <summary>
    /// Static training flag for ML-Agents autonomous training. Set from command-line (-training)
    /// so it persists across scene reloads when the Game scene is reloaded for the next episode.
    /// </summary>
    public static class TrainingMode
    {
        const string TrainingFlag = "-training";

        static bool? _cached;

        /// <summary>
        /// True when the game was launched with -training (e.g. for headless ML-Agents training).
        /// All players will be RL agents and the arena will reload on round end instead of going to Standings.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                if (_cached.HasValue)
                    return _cached.Value;
                _cached = Environment.GetCommandLineArgs().Any(
                    arg => string.Equals(arg, TrainingFlag, StringComparison.OrdinalIgnoreCase)
                );
                return _cached.Value;
            }
        }
    }
}
