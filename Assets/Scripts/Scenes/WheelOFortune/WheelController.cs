using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.WheelOFortune
{
    public class WheelController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform wheelPanel;   // Vertical Layout
        public GameObject rowPrefab;   // Prefab with Avatar + Pointer + Avatar

        [Header("Avatars")]
        public Sprite[] avatarSprites; // 5 sprites, one per player

        [Header("Wheel Settings")]
        [Header("Wheel Settings")]
        [Tooltip("Controls the spin frequency over time. X=time [0..1], Y=delay in seconds.")]
        public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0.01f, 1, 0.5f);
    
        [Tooltip("Minimum total spin duration (seconds)")]
        public float minSpinDuration = 2f;

        [Tooltip("Maximum total spin duration (seconds)")]
        public float maxSpinDuration = 5f;

        private float spinDuration; // chosen at runtime
    
        [Tooltip("Extra wait time before advancing to next scene")]
        [Range(0.5f, 5f)]
        public float postSpinDelay = 1.5f;
    
        private Transform[] rowPointers;
        private int playerCount;

        private void Start()
        {
            // Clear old rows
            foreach (Transform child in wheelPanel)
            {
                Destroy(child.gameObject);
            }

            // Build rows
            playerCount = PlayerPrefs.GetInt("Players", 2);
            rowPointers = new Transform[playerCount];

            for (int i = 1; i <= playerCount; i++)
            {
                GameObject row = Instantiate(rowPrefab, wheelPanel);

                // Avatar
                var avatar = row.transform.Find("Avatar").GetComponent<Image>();
                if (avatarSprites != null && avatarSprites.Length >= i)
                {
                    avatar.sprite = avatarSprites[i - 1];
                }

                // Pointer
                rowPointers[i - 1] = row.transform.Find("Pointer");
                rowPointers[i - 1].gameObject.SetActive(false); // start hidden
            }

            // Start spin
            StartCoroutine(SpinAndStop());
        }

        private IEnumerator SpinAndStop()
        {
            AudioController.I.ResetWheelTicks();
        
            spinDuration = Random.Range(minSpinDuration, maxSpinDuration);
        
            int index = 0;

            // Pick random stopping index
            int stopIndex = Random.Range(0, playerCount);

            // Spin loop
            float elapsed = 0f;

            while (elapsed < spinDuration)
            {
                // Hide all pointers
                for (int i = 0; i < playerCount; i++)
                    rowPointers[i].gameObject.SetActive(false);

                // Show current pointer
                rowPointers[index].gameObject.SetActive(true);

                // Play tick sound
                AudioController.I.PlayWheelTick();

                // Evaluate delay from curve
                float t = elapsed / spinDuration; // 0..1
                float delay = spinCurve.Evaluate(t);

                yield return new WaitForSeconds(delay);

                index = (index + 1) % playerCount;
                elapsed += delay;
            }

            // Final reward sound
            AudioController.I.PlayChaChing();

            // Reward selected player with a coin
            int winningPlayer = stopIndex + 1;
            int coins = PlayerPrefs.GetInt($"Player{winningPlayer}_Coins", 0);
            PlayerPrefs.SetInt($"Player{winningPlayer}_Coins", coins + 1);
            PlayerPrefs.Save();

            Debug.Log($"Player {winningPlayer} wins a coin! Total: {coins + 1}");

            // Wait before moving on
            yield return new WaitForSeconds(postSpinDelay);

            SceneFlowManager.I.SignalScreenDone();
        }

    }
}
