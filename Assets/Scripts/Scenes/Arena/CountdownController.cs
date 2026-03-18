using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Arena
{
    public class CountdownController : MonoBehaviour
    {
        public Text  countdownText;
        public float interval = 1f; // seconds between counts (visual only)

        private void OnEnable()  => AudioController.OnOneShotComplete += OnMusicFinished;
        private void OnDisable() => AudioController.OnOneShotComplete -= OnMusicFinished;

        private void Start()
        {
            float clipLength = AudioController.I != null ? AudioController.I.ActiveClipLength : 0f;
            if (clipLength > 0f)
                interval = clipLength / 3f;

            StartCoroutine(RunVisualCountdown());
        }

        // Drives the visual 3-2-1 display independently of the music duration
        IEnumerator RunVisualCountdown()
        {
            int count = 3;
            while (count > 0)
            {
                countdownText.text = count.ToString();
                yield return new WaitForSeconds(interval);
                count--;
            }
            countdownText.text = "";
        }

        // Scene ends when the music clip finishes — length of scene matches length of clip
        private void OnMusicFinished()
        {
            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
