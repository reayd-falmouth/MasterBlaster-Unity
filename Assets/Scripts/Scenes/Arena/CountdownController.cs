using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Arena
{
    public class CountdownController : MonoBehaviour
    {
        public Text countdownText; // Assign in inspector
        public float interval = 1f; // seconds between counts

        private void Start()
        {
            StartCoroutine(RunCountdown());
        }

        IEnumerator RunCountdown()
        {
            int count = 3;
            float t0 = Time.realtimeSinceStartup;

            while (count > 0)
            {
                countdownText.text = count.ToString();
                float tShow = Time.realtimeSinceStartup - t0;
                Debug.Log($"[Countdown] Showing '{count}' at t={tShow:F2}s, waiting {interval}s");
                yield return new WaitForSeconds(interval);
                float tAfterWait = Time.realtimeSinceStartup - t0;
                Debug.Log($"[Countdown] Wait finished for '{count}' at t={tAfterWait:F2}s (waited {tAfterWait - tShow:F2}s)");
                count--;
            }

            Debug.Log($"[Countdown] SignalScreenDone at t={Time.realtimeSinceStartup - t0:F2}s");
            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
