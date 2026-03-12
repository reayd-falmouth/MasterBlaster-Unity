using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;

namespace Scenes.Arena
{
    /// <summary>
    /// When in training mode, subscribes to the ML-Agents Academy environment reset.
    /// On reset (e.g. after max steps), reloads the Game scene so the next episode starts clean.
    /// Add this component to the Game scene (e.g. on an empty GameObject or GameManager).
    /// </summary>
    public class TrainingAcademyHelper : MonoBehaviour
    {
        private void Start()
        {
            if (!TrainingMode.IsActive)
                return;

            if (Academy.Instance != null)
            {
                Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
            }
        }

        private void OnDestroy()
        {
            if (Academy.IsInitialized)
                Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
        }

        private void OnEnvironmentReset()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
