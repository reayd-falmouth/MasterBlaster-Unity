using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scenes.Arena
{
    /// <summary>
    /// Static training flag for ML-Agents autonomous training. Active when:
    /// - the active scene is named "Train", or
    /// - the game was launched with -training (e.g. headless build).
    /// When active, all players are RL agents and the arena reloads on round end instead of going to Standings.
    /// </summary>
    public static class TrainingMode
    {
        const string TrainingFlag = "-training";
        const string TrainSceneName = "Train";

        static bool? _cached;

        /// <summary>
        /// True when running in the Train scene or when launched with -training.
        /// All players will be RL agents and the arena will reload on round end instead of going to Standings.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                if (_cached.HasValue)
                    return _cached.Value;
                if (Environment.GetCommandLineArgs().Any(
                    arg => string.Equals(arg, TrainingFlag, StringComparison.OrdinalIgnoreCase)))
                {
                    _cached = true;
                    return true;
                }
                if (Application.isPlaying && SceneManager.GetActiveScene().name.IndexOf(TrainSceneName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _cached = true;
                    return true;
                }
                _cached = false;
                return false;
            }
        }
    }
}
