using System.Collections;
using Core;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.WheelOFortune
{
    public class WheelController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform wheelPanel; // Vertical Layout
        public GameObject rowPrefab; // Prefab with Avatar + Pointer + Avatar

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

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player tickFeedbacks;
        [SerializeField] private MMF_Player rewardFeedbacks;

        private Transform[] rowPointers;
        private int playerCount;

        private void Start()
        {
            // How many rows exist under the panel (e.g. 5)
            int maxRows = wheelPanel.childCount;
            // How many players are actually in the game
            playerCount = PlayerPrefs.GetInt("Players", 2);
            playerCount = Mathf.Clamp(playerCount, 0, maxRows);
            rowPointers = new Transform[playerCount];
            for (int i = 0; i < maxRows; i++)
            {
                Transform row = wheelPanel.GetChild(i);
                bool isActive = i < playerCount;
                row.gameObject.SetActive(isActive);
                if (!isActive)
                    continue;
                // Set avatar sprite
                var avatar = row.Find("Avatar").GetComponent<Image>();
                if (avatarSprites != null && avatarSprites.Length > i)
                {
                    avatar.sprite = avatarSprites[i];
                }
                // Store pointer and start hidden
                rowPointers[i] = row.Find("Pointer");
                rowPointers[i].gameObject.SetActive(false);
            }
            // Start spin
            StartCoroutine(SpinAndStop());
        }

        private IEnumerator SpinAndStop()
        {

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

                tickFeedbacks?.PlayFeedbacks();
                
                // Evaluate delay from curve
                float t = elapsed / spinDuration; // 0..1
                float delay = spinCurve.Evaluate(t);

                yield return new WaitForSeconds(delay);

                index = (index + 1) % playerCount;
                elapsed += delay;
            }

            rewardFeedbacks?.PlayFeedbacks();

            // Reward selected player with a coin (session-only, in SessionManager)
            int winningPlayer = stopIndex + 1;
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.AddCoins(winningPlayer, 1);
                int total = SessionManager.Instance.GetCoins(winningPlayer);
                Debug.Log($"Player {winningPlayer} wins a coin! Total: {total}");
            }

            // Wait before moving on
            yield return new WaitForSeconds(postSpinDelay);

            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
