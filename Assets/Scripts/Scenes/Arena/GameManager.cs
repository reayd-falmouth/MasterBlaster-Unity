using System;
using System.Collections;
using Core;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player;
using Scenes.Arena.Player.AI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.MLAgents;
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

        [Header("AI")]
        [Tooltip("If true, AI players use reinforcement learning (ML-Agents). Requires Behavior Parameters on agent and a trained model for best results. If false, uses scripted AI.")]
        [SerializeField] private bool useReinforcementLearning;

        [Tooltip("Optional: assign the Training Academy GameObject (the one with the ML-Agents Academy component) for reference. Not used for logic; Academy is accessed via Academy.Instance.")]
        [SerializeField] private GameObject trainingAcademyObject;

        [Header("Input")]
        [Tooltip("Assign PlayerControls (or UIMenus) here so human players can use gamepad/keyboard. If empty, we try Resources.Load('PlayerControls').")]
        [SerializeField]
        private InputActionAsset playerInputActions;

        [Header("Assign the 5 players in inspector")]
        public GameObject topLeftPlayer;
        public GameObject topRightPlayer;
        public GameObject bottomLeftPlayer;
        public GameObject bottomRightPlayer;
        public GameObject middlePlayer;

        /// <summary>Prevents AddWin and transition from running more than once per round (e.g. when multiple deaths trigger CheckWinState).</summary>
        private bool _roundEndProcessed;

        private void Start()
        {
            _roundEndProcessed = false;
            float t0 = Time.realtimeSinceStartup;
            Debug.Log($"[GameManager] Start() began at t={t0:F2}s (Game scene active; Awake/OnEnable already ran)");
            if (TrainingMode.IsActive)
                Debug.Log("[GameManager] ML-Agents training: this scene (Game/Train) must be the one that loads when you press Play, or the Python trainer will timeout. Open the Game (or Train) scene, then press Play.");

            // Use assigned references so we have all 5 players even when some start inactive (AI needs GetPlayers() to find opponents)
            players = new[]
            {
                topLeftPlayer,
                topRightPlayer,
                bottomLeftPlayer,
                bottomRightPlayer,
                middlePlayer
            };
            int playerCount = TrainingMode.IsActive ? 2 : PlayerPrefs.GetInt("Players", 2);

            // Ensure SessionManager has structure for this game (e.g. first round from menu); do not re-initialize when returning from shop
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

            // Controller check is done in Menu; empty slots become AI. When SessionManager is null
            // (e.g. Game scene opened directly), we skip device assignment so AttachInputProvider gives all slots AI.
            if (!TrainingMode.IsActive && SessionManager.Instance != null)
                SessionManager.Instance.AssignInputDevices(playerCount);

            SetupPlayers(playerCount);

            // 🔹 Force upgrades and wins to be reapplied every new game round (only for players in this game)
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerController>();
                if (pc != null && pc.playerId > 0 && SessionManager.Instance != null)
                {
                    pc.wins = SessionManager.Instance.GetWins(pc.playerId);
                    pc.ApplyUpgrades();
                    var bc = p.GetComponent<BombController>();
                    if (bc != null)
                        bc.ApplyUpgrades(pc.playerId);
                }
            }

            // Load settings; in training mode override so shrinking is off and we use stable arena
            if (TrainingMode.IsActive)
            {
                shrinkingEnabled = false;
                normalLevel = true;
                startMoney = false;
            }
            else
            {
                shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;
                normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
                startMoney = PlayerPrefs.GetInt("StartMoney", 0) == 1;
            }

            if (!normalLevel)
                LoadAlternateLevelSettings();

            // Start money only on first game from menu, not when returning from shop for another round
            if (startMoney && PlayerPrefs.GetInt("GiveStartMoneyNextArena", 0) == 1)
            {
                GivePlayersStartCoin();
                PlayerPrefs.SetInt("GiveStartMoneyNextArena", 0);
                PlayerPrefs.Save();
            }

            Debug.Log($"[GameManager] Start() finished at t={Time.realtimeSinceStartup:F2}s (took {Time.realtimeSinceStartup - t0:F2}s)");
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
                movement.wins =
                    SessionManager.Instance != null ? SessionManager.Instance.GetWins(id) : 0;
            }

            AttachInputProvider(playerObj, id, movement);

            playerObj.SetActive(true);
        }

        private void AttachInputProvider(GameObject playerObj, int id, PlayerController movement)
        {
            // Remove any existing input components so we don't duplicate when re-entering scene
            var existingHuman = playerObj.GetComponent<HumanPlayerInput>();
            if (existingHuman != null)
                Destroy(existingHuman);
            var existingAI = playerObj.GetComponent<AIPlayerInput>();
            if (existingAI != null)
                Destroy(existingAI);
            var existingBrain = playerObj.GetComponent<ScriptedAIBrain>();
            if (existingBrain != null)
                Destroy(existingBrain);
            var existingMLAgent = playerObj.GetComponent<BombermanAgent>();
            if (existingMLAgent != null)
                Destroy(existingMLAgent);
            var existingMLBrain = playerObj.GetComponent<MLAgentsBrain>();
            if (existingMLBrain != null)
                Destroy(existingMLBrain);

            // In training mode, all players are RL agents (no human)
            int? device = TrainingMode.IsActive ? null : (SessionManager.Instance != null ? SessionManager.Instance.GetAssignedDevice(id) : null);

            if (device.HasValue && movement != null)
            {
                var human = playerObj.AddComponent<HumanPlayerInput>();
                var asset = playerInputActions != null ? playerInputActions : Resources.Load<InputActionAsset>("PlayerControls");
                if (asset != null)
                    human.inputActions = asset;
                else
                    Debug.LogWarning("[GameManager] No Input Action Asset for human player. Assign GameManager's 'Player Input Actions' in the Game scene, or put PlayerControls.inputactions in a Resources folder.");
                human.Init(
                    device.Value,
                    movement.inputUp,
                    movement.inputDown,
                    movement.inputLeft,
                    movement.inputRight,
                    playerObj.GetComponent<BombController>()?.inputKey ?? KeyCode.LeftShift,
                    playerObj.GetComponent<BombController>()?.inputKey ?? KeyCode.LeftShift
                );
            }
            else
            {
                // In training mode always use RL agents (Academy is created when agents register). Otherwise require Academy.Instance.
                bool useRL = TrainingMode.IsActive || (useReinforcementLearning && Academy.Instance != null);
                if (useRL)
                {
                    var agent = playerObj.AddComponent<BombermanAgent>();
                    var mlBrain = playerObj.AddComponent<MLAgentsBrain>();
                    var aiInput = playerObj.AddComponent<AIPlayerInput>();
                    aiInput.Init(mlBrain);
                    Debug.Log($"[GameManager] Player {id} → RL (BombermanAgent + MLAgentsBrain)");
                }
                else
                {
                    var brain = playerObj.AddComponent<ScriptedAIBrain>();
                    var aiInput = playerObj.AddComponent<AIPlayerInput>();
                    aiInput.Init(brain);
                    Debug.Log($"[GameManager] Player {id} → ScriptedAIBrain");
                }
            }
        }

        public void CheckWinState()
        {
            if (players == null || players.Length == 0)
                return;
            if (_roundEndProcessed)
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
            int currentWinsOfLastAlive =
                (lastAlivePlayerId != 0 && SessionManager.Instance != null)
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
                _roundEndProcessed = true;
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
                    int winsAfterThisRound =
                        SessionManager.Instance != null
                            ? SessionManager.Instance.GetWins(movement.playerId)
                            : movement.wins;
                    bool shouldGoToOvers = winsAfterThisRound >= winsNeeded;

                    if (result.Outcome == WinOutcome.GoToOvers || shouldGoToOvers)
                    {
                        if (TrainingMode.IsActive)
                        {
                            var winnerAgent = lastAlive.GetComponent<BombermanAgent>();
                            if (winnerAgent != null)
                                winnerAgent.AddReward(0.5f);
                            ReloadGameSceneAfterDelay(1.5f);
                            return;
                        }
                        if (SessionManager.Instance != null)
                            SessionManager.Instance.SetMatchWinner(
                                movement.playerId,
                                lastAlive.name
                            );
                        SceneFlowManager.I.GoToOvers();
                        return;
                    }
                }
            }

            if (result.Outcome != WinOutcome.NoChange)
                _roundEndProcessed = true;

            if (TrainingMode.IsActive)
                ReloadGameSceneAfterDelay(1.5f);
            else if (PlayerPrefs.GetInt("QuickRestart", 0) == 1)
                ReloadRoundAfterDelay(3f);
            else
                Invoke(nameof(Standings), 3f);
        }

        private void ReloadGameSceneAfterDelay(float delay)
        {
            Invoke(nameof(ReloadGameScene), delay);
        }

        private void ReloadGameScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Load Countdown scene to start the next round (skip Standings/Shop). Used when QuickRestart is enabled.</summary>
        private void ReloadRoundAfterDelay(float delay)
        {
            Invoke(nameof(GoToCountdown), delay);
        }

        private void GoToCountdown()
        {
            SceneManager.LoadScene("Countdown");
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
            if (TrainingMode.IsActive)
            {
                ReloadGameSceneAfterDelay(1.5f);
                return;
            }
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
