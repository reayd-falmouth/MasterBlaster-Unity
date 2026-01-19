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

            while (count > 0)
            {
                countdownText.text = count.ToString();
                yield return new WaitForSeconds(interval);
                count--;
            }

            SceneFlowManager.I.SignalScreenDone();
        }
    }
}