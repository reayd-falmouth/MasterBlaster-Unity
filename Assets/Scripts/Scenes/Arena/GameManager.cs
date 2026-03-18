using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Online;
using Scenes.Arena.Bomb;
using Scenes.Arena.Map;
using Scenes.Arena.Player;
using Scenes.Arena.Player.AI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

namespace Scenes.Arena
{
    [DefaultExecutionOrder(-1)]
    public class GameManager : NetworkBehaviour
    {
        /// <summary>
        /// Convenience accessor for single-arena scenes. In multi-arena training each arena has its own
        /// GameManager — use local references (cached via transform.root) instead of this property there.
        /// </summary>
        public static GameManager Instance { get; private set; }

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

        [Tooltip("When true, RL agents use Heuristic so they move without the Python trainer. When false, use Default so the trainer sends actions (for training); ensure Game scene loads when you press Play.")]
        [SerializeField] private bool useHeuristicOnlyForAgents = true;

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

        // ── Online multiplayer ───────────────────────────────────────────────────────
        /// <summary>Maps NGO client IDs → arena player IDs. Host-only.</summary>
        private readonly Dictionary<ulong, int> _clientToPlayerId = new Dictionary<ulong, int>();

        // ── Training episode reset ───────────────────────────────────────────────────
        // Captured once in Start() so ResetArenaForTraining() can restore the arena
        // to its initial state without reloading the scene.
        private Tilemap _destructibleTilemap;
        private readonly Dictionary<Vector3Int, TileBase> _initialDestructibleTiles = new Dictionary<Vector3Int, TileBase>();
        private readonly Dictionary<GameObject, Vector3> _initialPlayerLocalPositions = new Dictionary<GameObject, Vector3>();

        // ── Arena object registries ──────────────────────────────────────────────────
        // BombInfo, Explosion, and ItemPickup self-register here so agents can do an
        // O(1) list read instead of a scene-wide FindObjectsByType scan every step.
        private readonly List<BombInfo>   _arenaBombs      = new List<BombInfo>();
        private readonly List<Explosion>  _arenaExplosions  = new List<Explosion>();
        private readonly List<ItemPickup> _arenaItems       = new List<ItemPickup>();

        public IReadOnlyList<BombInfo>   ArenaBombs      => _arenaBombs;
        public IReadOnlyList<Explosion>  ArenaExplosions  => _arenaExplosions;
        public IReadOnlyList<ItemPickup> ArenaItems       => _arenaItems;

        public void RegisterBomb(BombInfo b)         { if (b != null && !_arenaBombs.Contains(b))       _arenaBombs.Add(b); }
        public void UnregisterBomb(BombInfo b)       => _arenaBombs.Remove(b);
        public void RegisterExplosion(Explosion e)   { if (e != null && !_arenaExplosions.Contains(e))  _arenaExplosions.Add(e); }
        public void UnregisterExplosion(Explosion e) => _arenaExplosions.Remove(e);
        public void RegisterItem(ItemPickup item)    { if (item != null && !_arenaItems.Contains(item)) _arenaItems.Add(item); }
        public void UnregisterItem(ItemPickup item)  => _arenaItems.Remove(item);

        private void Awake()
        {
            // Soft singleton: first arena wins. Multi-arena training scenes use local references instead.
            if (Instance == null)
                Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

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

            if (TrainingMode.IsActive)
                CaptureInitialArenaState();

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

                // Lock this player to their specific gamepad so controllers don't cross-control.
                int gpIndex = device.Value - 1;
                if (gpIndex >= 0 && gpIndex < Gamepad.all.Count)
                    human.SetGamepad(Gamepad.all[gpIndex]);
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
                    // BehaviorType is set by BombermanAgent.EnsureBehaviorParameters() based on TrainingMode.IsActive.
                    Debug.Log($"[GameManager] Player {id} → RL (BombermanAgent + MLAgentsBrain), TrainingMode={TrainingMode.IsActive}");
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
            // In online play, only the host drives win-state and scene transitions.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
                return;

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
                            return;   // scene reload handled by Academy / TrainingAcademyHelper
                        }
                        if (SessionManager.Instance != null)
                            SessionManager.Instance.SetMatchWinner(
                                movement.playerId,
                                lastAlive.name
                            );
                        if (IsNetworked)
                            GoToOversClientRpc();
                        else
                            SceneFlowManager.I.GoToOvers();
                        return;
                    }
                }
            }

            if (result.Outcome != WinOutcome.NoChange)
                _roundEndProcessed = true;

            if (TrainingMode.IsActive)
                return;   // scene reload handled by Academy / TrainingAcademyHelper
            else if (PlayerPrefs.GetInt("QuickRestart", 0) == 1)
                ReloadRoundAfterDelay(3f);
            else
                Invoke(nameof(Standings), 3f);
        }

        private void ReloadGameSceneAfterDelay(float delay)
        {
            Invoke(nameof(ReloadGameScene), delay);
        }

        private void CaptureInitialArenaState()
        {
            // Save spawn positions for all initially-active players.
            _initialPlayerLocalPositions.Clear();
            foreach (var p in players)
                if (p != null && p.activeInHierarchy)
                    _initialPlayerLocalPositions[p] = p.transform.localPosition;

            // Save every tile in the destructible tilemap.
            var bc = topLeftPlayer != null ? topLeftPlayer.GetComponent<BombController>() : null;
            if (bc != null && bc.destructibleTiles != null)
            {
                _destructibleTilemap = bc.destructibleTiles;
                _initialDestructibleTiles.Clear();
                foreach (var pos in _destructibleTilemap.cellBounds.allPositionsWithin)
                {
                    var tile = _destructibleTilemap.GetTile(pos);
                    if (tile != null) _initialDestructibleTiles[pos] = tile;
                }
            }
        }

        /// <summary>
        /// Resets this arena for a new training episode without reloading the scene.
        /// Called from BombermanAgent.OnEpisodeBegin().
        /// </summary>
        public void ResetArenaForTraining()
        {
            _roundEndProcessed = false;

            // Destroy all live bombs, explosions, and items.
            for (int i = _arenaBombs.Count - 1; i >= 0; i--)
                if (_arenaBombs[i] != null) Destroy(_arenaBombs[i].gameObject);
            _arenaBombs.Clear();

            for (int i = _arenaExplosions.Count - 1; i >= 0; i--)
                if (_arenaExplosions[i] != null) Destroy(_arenaExplosions[i].gameObject);
            _arenaExplosions.Clear();

            for (int i = _arenaItems.Count - 1; i >= 0; i--)
                if (_arenaItems[i] != null) Destroy(_arenaItems[i].gameObject);
            _arenaItems.Clear();

            // Restore the destructible tilemap to its initial layout.
            if (_destructibleTilemap != null && _initialDestructibleTiles.Count > 0)
            {
                _destructibleTilemap.ClearAllTiles();
                foreach (var kvp in _initialDestructibleTiles)
                    _destructibleTilemap.SetTile(kvp.Key, kvp.Value);
            }

            // Re-enable players at their original spawn positions.
            foreach (var kvp in _initialPlayerLocalPositions)
            {
                var p = kvp.Key;
                if (p == null) continue;

                var pc = p.GetComponent<PlayerController>();
                if (pc != null) pc.ResetForEpisode();

                p.transform.localPosition = kvp.Value;
                if (!p.activeInHierarchy) p.SetActive(true);
                if (pc != null) pc.enabled = true;

                var bc = p.GetComponent<BombController>();
                if (bc != null) bc.enabled = true; // OnEnable resets bombsRemaining
            }
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

        /// <summary>True when NGO is active (host/server/client session running).</summary>
        private bool IsNetworked =>
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        private void Standings()
        {
            SyncCoinsToSessionManager();
            if (IsNetworked)
                GoToStandingsClientRpc();
            else
                SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        // ------------------- Custom behaviours -------------------

        private void EndGame()
        {
            Debug.Log("[GameManager] Timer expired → game over!");
            if (TrainingMode.IsActive)
                return;   // individual episode resets are handled by Academy / BombermanAgent.OnEpisodeBegin
            if (IsNetworked && !IsServer)
                return;
            SyncCoinsToSessionManager();
            if (IsNetworked)
                GoToStandingsClientRpc();
            else
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

        // ── Online: client-to-player mapping ────────────────────────────────────────

        /// <summary>Host-only: register which arena player ID belongs to a connected client.</summary>
        public void AssignNetworkClient(ulong clientId, int playerId)
        {
            _clientToPlayerId[clientId] = playerId;
        }

        // ── Online: RPCs ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Clients call this every FixedUpdate to send their input to the host.
        /// The host pushes it into that player's NetworkPlayerInput component.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SendInputServerRpc(Vector2 move, bool bombDown, bool detonate, ServerRpcParams rpc = default)
        {
            ulong clientId = rpc.Receive.SenderClientId;
            if (!_clientToPlayerId.TryGetValue(clientId, out int playerId))
                return;

            foreach (var p in players)
            {
                if (p == null) continue;
                var pc = p.GetComponent<Player.PlayerController>();
                if (pc == null || pc.playerId != playerId) continue;
                var netInput = p.GetComponent<Online.NetworkPlayerInput>();
                netInput?.ReceiveInput(move, bombDown, detonate);
                break;
            }
        }

        [ClientRpc]
        private void GoToStandingsClientRpc()
        {
            SyncCoinsToSessionManager();
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        [ClientRpc]
        private void GoToOversClientRpc()
        {
            if (SessionManager.Instance != null)
            {
                // Ensure winner info is propagated — host already set it
            }
            SceneFlowManager.I.GoToOvers();
        }

        /// <summary>
        /// Respawns a single player at their initial spawn position without disturbing
        /// the arena tiles, bombs, or other players. Used when an agent dies but opponents
        /// are still alive.
        /// </summary>
        public void ResetSinglePlayerForTraining(GameObject player)
        {
            if (!_initialPlayerLocalPositions.TryGetValue(player, out var spawnPos)) return;

            player.transform.localPosition = spawnPos;

            var pc = player.GetComponent<PlayerController>();
            if (pc != null) { pc.ResetForEpisode(); pc.enabled = true; }

            var bc = player.GetComponent<BombController>();
            if (bc != null) bc.enabled = true;
        }
    }
}
