using System;
using System.Collections;
using Core;
using Scenes.Arena.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Utilities;

namespace Scenes.Arena
{
    [DefaultExecutionOrder(-1)]
    public class GameManager : Singleton<GameManager>
    {
        // public static GameManager Instance { get; private set; }

        [SerializeField]
        private GameObject[] players;

        [SerializeField]
        private bool shrinkingEnabled;

        [SerializeField]
        private bool normalLevel;

        [SerializeField]
        private bool startMoney;

        [Header("Assign the 5 players in inspector")]
        public GameObject topLeftPlayer;
        public GameObject topRightPlayer;
        public GameObject bottomLeftPlayer;
        public GameObject bottomRightPlayer;
        public GameObject middlePlayer;

        private void Start()
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            int playerCount = PlayerPrefs.GetInt("Players", 2);

            // Ensure SessionManager has structure for this game (e.g. first round from menu); do not re-initialize when returning from shop
            if (
                SessionManager.Instance.PlayerUpgrades == null
                || SessionManager.Instance.PlayerUpgrades.Count == 0
                || !SessionManager.Instance.PlayerUpgrades.ContainsKey(1)
            )
            {
                SessionManager.Instance.Initialize(playerCount);
            }

            SetupPlayers(playerCount);

            // 🔹 Force upgrades to be reapplied every new game round
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                    pc.ApplyUpgrades();
            }

            // Load settings
            shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;
            normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            startMoney = PlayerPrefs.GetInt("StartMoney", 0) == 1;

            if (!normalLevel)
                LoadAlternateLevelSettings();

            if (startMoney)
                GivePlayersStartCoin();
        }

        void SetupPlayers(int count)
        {
            topLeftPlayer.SetActive(false);
            topRightPlayer.SetActive(false);
            bottomLeftPlayer.SetActive(false);
            bottomRightPlayer.SetActive(false);
            middlePlayer.SetActive(false);

            var setup = ArenaLogic.GetPlayerSetup(count);
            foreach (var (slot, playerId) in setup)
            {
                var playerObj = GetPlayerObject(slot);
                if (playerObj != null)
                    EnablePlayer(playerObj, playerId);
            }
        }

        GameObject GetPlayerObject(PlayerSlot slot)
        {
            switch (slot)
            {
                case PlayerSlot.TopLeft:
                    return topLeftPlayer;
                case PlayerSlot.TopRight:
                    return topRightPlayer;
                case PlayerSlot.BottomLeft:
                    return bottomLeftPlayer;
                case PlayerSlot.BottomRight:
                    return bottomRightPlayer;
                case PlayerSlot.Middle:
                    return middlePlayer;
                default:
                    return null;
            }
        }

        private void EnablePlayer(GameObject playerObj, int id)
        {
            playerObj.SetActive(true);
            var movement = playerObj.GetComponent<PlayerController>();
            if (movement != null)
                movement.playerId = id;
        }

        public void CheckWinState()
        {
            if (players == null || players.Length == 0)
                return;

            var playerActive = new bool[players.Length];
            int currentWinsOfLastAlive = 0;
            for (int i = 0; i < players.Length; i++)
            {
                playerActive[i] = players[i].activeSelf;
                if (players[i].activeSelf)
                {
                    var pc = players[i].GetComponent<PlayerController>();
                    if (pc != null)
                        currentWinsOfLastAlive = pc.wins;
                }
            }

            int winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
            var result = ArenaLogic.EvaluateWinState(
                playerActive,
                currentWinsOfLastAlive,
                winsNeeded
            );

            if (result.Outcome == WinOutcome.NoChange)
                return;

            if (result.LastAliveIndex.HasValue)
            {
                var lastAlive = players[result.LastAliveIndex.Value];
                var movement = lastAlive.GetComponent<PlayerController>();
                if (movement != null)
                {
                    movement.wins++;
                    PlayerPrefs.SetInt(lastAlive.name + "_Wins", movement.wins);
                    PlayerPrefs.Save();
                    if (result.Outcome == WinOutcome.GoToOvers)
                    {
                        PlayerPrefs.SetString("WinnerName", lastAlive.name);
                        PlayerPrefs.Save();
                        SceneFlowManager.I.GoToOvers();
                        return;
                    }
                }
            }

            Invoke(nameof(Standings), 3f);
        }

        private void Standings()
        {
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        // ------------------- Custom behaviours -------------------

        private void EndGame()
        {
            Debug.Log("[GameManager] Timer expired → game over!");
            // Option A: go directly to Standings
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        void LoadAlternateLevelSettings()
        {
            Debug.Log("[GameManager] NormalLevel disabled → loading alternate map settings");
            // TODO: load alternate spawn points, layouts, etc.
        }

        void GivePlayersStartCoin()
        {
            foreach (var p in players)
            {
                var movement = p.GetComponent<PlayerController>();
                if (movement != null)
                {
                    movement.coins += 1;
                    Debug.Log($"{p.name} given 1 start coin (total: {movement.coins})");
                }
            }
        }

        public GameObject[] GetPlayers() => players;
    }
}
