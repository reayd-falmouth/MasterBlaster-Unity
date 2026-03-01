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
            // SessionManager may not exist in the Game scene (e.g. Menu→Countdown→Game); if null, skip so SetupPlayers still runs and only N players show
            if (
                SessionManager.Instance != null
                && (
                    SessionManager.Instance.PlayerUpgrades == null
                    || SessionManager.Instance.PlayerUpgrades.Count == 0
                    || !SessionManager.Instance.PlayerUpgrades.ContainsKey(1)
                )
            )
            {
                SessionManager.Instance.Initialize(playerCount);
            }

            SetupPlayers(playerCount);

            // 🔹 Force upgrades and wins to be reapplied every new game round (only for players in this game)
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null && pc.playerId > 0 && SessionManager.Instance != null)
                {
                    pc.wins = SessionManager.Instance.GetWins(pc.playerId);
                    pc.ApplyUpgrades();
                }
            }

            // Load settings
            shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;
            normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            startMoney = PlayerPrefs.GetInt("StartMoney", 0) == 1;

            if (!normalLevel)
                LoadAlternateLevelSettings();

            // Start money only on first game from menu, not when returning from shop for another round
            if (startMoney && PlayerPrefs.GetInt("GiveStartMoneyNextArena", 0) == 1)
            {
                GivePlayersStartCoin();
                PlayerPrefs.SetInt("GiveStartMoneyNextArena", 0);
                PlayerPrefs.Save();
            }
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
            var movement = playerObj.GetComponent<PlayerController>();
            if (movement != null)
            {
                movement.playerId = id;
                movement.wins = SessionManager.Instance != null ? SessionManager.Instance.GetWins(id) : 0;
            }
            playerObj.SetActive(true);
        }

        public void CheckWinState()
        {
            if (players == null || players.Length == 0)
                return;

            var playerActive = new bool[players.Length];
            int lastAlivePlayerId = 0;
            int lastAlivePcWins = 0;
            for (int i = 0; i < players.Length; i++)
            {
                playerActive[i] = players[i].activeSelf;
                if (players[i].activeSelf)
                {
                    var pc = players[i].GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        lastAlivePlayerId = pc.playerId;
                        lastAlivePcWins = pc.wins;
                    }
                }
            }

            // Use SessionManager as source of truth for win count when deciding Overs vs Standings
            int currentWinsOfLastAlive = (lastAlivePlayerId != 0 && SessionManager.Instance != null)
                ? SessionManager.Instance.GetWins(lastAlivePlayerId)
                : lastAlivePcWins;

            int winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
            var result = ArenaLogic.EvaluateWinState(
                playerActive,
                currentWinsOfLastAlive,
                winsNeeded
            );

            if (result.Outcome == WinOutcome.NoChange)
                return;

            // When exactly one player is left, we always transition to Standings or Overs.
            if (result.LastAliveIndex.HasValue)
            {
                var lastAlive = players[result.LastAliveIndex.Value];
                var movement = lastAlive.GetComponent<PlayerController>();
                if (movement != null)
                {
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.AddWin(movement.playerId);
                        movement.wins = SessionManager.Instance.GetWins(movement.playerId);
                    }
                    else
                        movement.wins++;

                    // Defensive: re-check from SessionManager in case we were given stale wins
                    int winsAfterThisRound = SessionManager.Instance != null
                        ? SessionManager.Instance.GetWins(movement.playerId)
                        : movement.wins;
                    bool shouldGoToOvers = winsAfterThisRound >= winsNeeded;

                    if (result.Outcome == WinOutcome.GoToOvers || shouldGoToOvers)
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
            SyncCoinsToSessionManager();
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        // ------------------- Custom behaviours -------------------

        private void EndGame()
        {
            Debug.Log("[GameManager] Timer expired → game over!");
            SyncCoinsToSessionManager();
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        void LoadAlternateLevelSettings()
        {
            Debug.Log("[GameManager] NormalLevel disabled → loading alternate map settings");
            // TODO: load alternate spawn points, layouts, etc.
        }

        void GivePlayersStartCoin()
        {
            if (SessionManager.Instance == null)
                return;
            int count = PlayerPrefs.GetInt("Players", 2);
            var setup = ArenaLogic.GetPlayerSetup(count);
            foreach (var (slot, playerId) in setup)
            {
                var playerObj = GetPlayerObject(slot);
                if (playerObj == null)
                    continue;
                var movement = playerObj.GetComponent<PlayerController>();
                if (movement != null)
                {
                    SessionManager.Instance.AddCoins(playerId, 1);
                    movement.coins = SessionManager.Instance.GetCoins(playerId);
                    Debug.Log(
                        $"{playerObj.name} (Player {playerId}) given 1 start coin (total: {movement.coins})"
                    );
                }
            }
        }

        /// <summary>Push each active player's in-memory coins to SessionManager so Shop sees current totals.</summary>
        void SyncCoinsToSessionManager()
        {
            if (SessionManager.Instance == null || players == null)
                return;
            foreach (var p in players)
            {
                if (p == null || !p.activeInHierarchy)
                    continue;
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                    SessionManager.Instance.SetCoins(pc.playerId, pc.coins);
            }
        }

        public GameObject[] GetPlayers() => players;
    }
}
