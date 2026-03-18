using System.Collections.Generic;
using Scenes.Arena;
using Scenes.Arena.Bomb;
using Scenes.Arena.Map;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Scenes.Arena.Player.AI
{
    /// <summary>
    /// ML-Agents reinforcement learning agent for Bomberman-style play.
    /// Observations: self position, nearest opponent, nearest bomb, danger, stats.
    /// Actions: move (5), place bomb (2), detonate (2).
    /// Rewards: survive step, kill opponent, collect item, die (negative).
    /// </summary>
    public class BombermanAgent : Agent
    {
        [Header("Rewards")]
        public float rewardPerStep = -0.001f;
        public float rewardKillOpponent = 1f;
        public float rewardCollectItem = 0.3f;
        public float rewardDeath = -1f;
        public float rewardPlaceBomb = 0f;
        public float rewardDestroyBlock = 0.3f;
        public float rewardInDangerPerStep = -0.003f;

        // Fix 2: gate all hot-path Debug.Log behind this flag (default off for training)
        [SerializeField] private bool verboseLogging = false;

        private const int ObservationSize = 61;
        private const float ArenaScale = 15f;

        // Fix 1: one static array replaces every inline new[] { Vector2.up, ... } allocation
        private static readonly Vector2[] Dirs4 = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        // Fix 3: BFS pools — allocated once per agent, cleared and reused each call
        private readonly Queue<Vector2>               _bfsQueue   = new Queue<Vector2>();
        private readonly HashSet<Vector2>             _bfsVisited = new HashSet<Vector2>();
        private readonly Dictionary<Vector2, Vector2> _bfsParent  = new Dictionary<Vector2, Vector2>();

        // Fix 4: safe-directions result pool — allocated once, cleared each GetSafeDirections call
        private readonly List<Vector2> _safeDirs = new List<Vector2>(4);

        // Last action output for MLAgentsBrain to read
        public Vector2 LastMove { get; private set; }
        public bool LastPlaceBomb { get; private set; }
        public bool LastDetonateHeld { get; private set; } = true;

        private int _opponentCountLastStep;
        private BombController _bombController;
        private PlayerController _playerController;

        // Arena-local scope: non-null when this agent lives under a shared arena root GameObject.
        // In multi-arena training scenes every arena is its own hierarchy; _arenaRoot lets us scope
        // GetComponentsInChildren searches to this arena only.
        private Transform _arenaRoot;
        private GameManager _localGameManager;

        protected override void Awake()
        {
            _bombController = GetComponent<BombController>();
            _playerController = GetComponent<PlayerController>();

            _arenaRoot = transform.root != transform ? transform.root : null;
            _localGameManager = (_arenaRoot != null
                ? _arenaRoot.GetComponentInChildren<GameManager>()
                : null) ?? GameManager.Instance;

            EnsureBehaviorParameters();
            base.Awake();
        }

        // Arena-scoped helpers — read from GameManager's pre-built registries (O(1), no allocation).
        // Falls back to scene-wide FindObjectsByType only in single-arena scenes with no local GM.
        private IReadOnlyList<BombInfo>   GetArenaBombs()     => _localGameManager != null ? _localGameManager.ArenaBombs     : (IReadOnlyList<BombInfo>)   FindObjectsByType<BombInfo>(FindObjectsSortMode.None);
        private IReadOnlyList<Explosion>  GetArenaExplosions() => _localGameManager != null ? _localGameManager.ArenaExplosions : (IReadOnlyList<Explosion>)  FindObjectsByType<Explosion>(FindObjectsSortMode.None);
        private IReadOnlyList<ItemPickup> GetArenaItems()      => _localGameManager != null ? _localGameManager.ArenaItems      : (IReadOnlyList<ItemPickup>) FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);

        private void EnsureBehaviorParameters()
        {
            var bp = GetComponent<BehaviorParameters>();
            if (bp == null)
            {
                bp = gameObject.AddComponent<BehaviorParameters>();
                bp.TeamId = 0;
            }
            bp.BehaviorName = "Bomberman";
            bp.BrainParameters.VectorObservationSize = ObservationSize;
            bp.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 5, 2, 2 });
            // Training mode: Default so agents register with the Python trainer and receive actions from the policy.
            // Otherwise: HeuristicOnly so our Heuristic() method runs (avoids zero-action inference with no model).
            bp.BehaviorType = TrainingMode.IsActive ? BehaviorType.Default : BehaviorType.HeuristicOnly;

            // Cap episode length so solo agents don't survive indefinitely without learning.
            if (TrainingMode.IsActive && MaxStep == 0)
                MaxStep = 3000; // ~60 s at 50 Hz FixedUpdate

            // DecisionRequester: Academy steps this agent every step so the trainer gets a timely response (avoids timeout).
            // DecisionPeriod 1 = request every Academy step so Unity responds quickly to the trainer.
            if (GetComponent<DecisionRequester>() == null)
            {
                var dr = gameObject.AddComponent<DecisionRequester>();
                dr.DecisionPeriod = 1;
                dr.TakeActionsBetweenDecisions = true;
            }
        }

        public override void OnEpisodeBegin()
        {
            if (!TrainingMode.IsActive)
                return;

            // If any opponent's PlayerController is still enabled, only respawn self —
            // don't disturb the arena or the surviving agent.
            bool anyOpponentAlive = false;
            if (_localGameManager != null)
            {
                foreach (var p in _localGameManager.GetPlayers())
                {
                    if (p == null || p.transform == transform) continue;
                    var pc = p.GetComponent<PlayerController>();
                    if (pc != null && pc.enabled) { anyOpponentAlive = true; break; }
                }
            }

            if (anyOpponentAlive)
                _localGameManager?.ResetSinglePlayerForTraining(gameObject);
            else
                _localGameManager?.ResetArenaForTraining();

            CountOpponents();
            _opponentCountLastStep = GetActiveOpponentCount();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CountOpponents();
            _opponentCountLastStep = GetActiveOpponentCount();
        }

        protected override void OnDisable()
        {
            // Base Agent.CleanupSensors() can throw NullReferenceException when the agent is disabled
            // before sensors are initialized (e.g. player death, scene unload). Guard to avoid crash.
            try
            {
                base.OnDisable();
            }
            catch (System.NullReferenceException)
            {
                // Agent/sensors not fully initialized; skip base cleanup
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (sensor == null) return;

            Vector2 myPos = transform.position;
            GameObject[] allPlayers = _localGameManager?.GetPlayers();

            // 0-1: my position normalized
            sensor.AddObservation(myPos.x / ArenaScale);
            sensor.AddObservation(myPos.y / ArenaScale);

            // 2-4: nearest opponent relative (dx, dy, distance) normalized
            GameObject nearest = GetNearestOther(allPlayers);
            if (nearest != null)
            {
                Vector2 delta = (Vector2)nearest.transform.position - myPos;
                float dist = delta.magnitude;
                sensor.AddObservation(Mathf.Clamp(delta.x / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(delta.y / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp01(dist / (ArenaScale * 2f)));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(1f);
            }

            // 5-6: own stats normalized
            if (_bombController != null)
            {
                sensor.AddObservation(Mathf.Clamp01(_bombController.bombAmount / 5f));
                sensor.AddObservation(Mathf.Clamp01(_bombController.explosionRadius / 5f));
            }
            else
            {
                sensor.AddObservation(0.2f);
                sensor.AddObservation(0.2f);
            }

            // 7-8: nearest item relative (if any)
            var item = GetNearestItem(myPos);
            if (item != null)
            {
                Vector2 delta = (Vector2)item.transform.position - myPos;
                sensor.AddObservation(Mathf.Clamp(delta.x / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(delta.y / ArenaScale, -1f, 1f));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }

            // 9-33: 5x5 local danger map centered on agent cell
            Vector2 agentCell = new Vector2(Mathf.Round(myPos.x), Mathf.Round(myPos.y));
            var bombs = GetArenaBombs();
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    sensor.AddObservation(GetCellDangerLevel(agentCell + new Vector2(dx, dy), bombs));

            // 34-58: 5x5 local structure map
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    sensor.AddObservation(GetCellStructure(agentCell + new Vector2(dx, dy)));

            // 59-60: padding (reserved for future use)
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        private float GetCellDangerLevel(Vector2 cell, IReadOnlyList<BombInfo> bombs)
        {
            var destr = _bombController?.destructibleTiles;
            var indestr = _bombController?.indestructibleTiles;
            float maxGradient = 0f;
            foreach (var b in bombs)
            {
                if (b == null) continue;
                Vector2 bc = new Vector2(Mathf.Round(b.transform.position.x), Mathf.Round(b.transform.position.y));
                bool threatened = false;
                if (cell == bc)
                {
                    threatened = true;
                }
                else
                {
                    // Fix 1: Dirs4 replaces inline new[] allocation
                    foreach (var d in Dirs4)
                    {
                        for (int i = 1; i <= b.explosionRadius; i++)
                        {
                            Vector2 check = bc + d * i;
                            if (!IsWalkable(check, destr, indestr)) break;
                            if (check == cell) { threatened = true; break; }
                        }
                        if (threatened) break;
                    }
                }
                if (threatened)
                {
                    float gradient = 1f - b.timeRemainingFraction; // 0 = just placed, 1 = imminent
                    if (gradient > maxGradient) maxGradient = gradient;
                }
            }
            if (IsCellUnderExplosion(cell)) return 1f;
            return maxGradient;
        }

        private float GetCellStructure(Vector2 cell)
        {
            var indestr = _bombController?.indestructibleTiles;
            var destr   = _bombController?.destructibleTiles;
            if (indestr != null && indestr.GetTile(indestr.WorldToCell(cell)) != null) return 1f;
            if (destr   != null && destr.GetTile(destr.WorldToCell(cell))     != null) return 0.5f;
            return 0f;
        }

        private float _lastOnActionLogTime = -999f;

        public override void OnActionReceived(ActionBuffers actions)
        {
            var discrete = actions.DiscreteActions;
            if (discrete.Length < 3) return;

            // Branch 0: move (0=none, 1=up, 2=down, 3=left, 4=right)
            int moveAction = discrete[0];
            LastMove = moveAction switch
            {
                1 => Vector2.up,
                2 => Vector2.down,
                3 => Vector2.left,
                4 => Vector2.right,
                _ => Vector2.zero
            };

            // Branch 1: place bomb (0=no, 1=yes)
            LastPlaceBomb = discrete[1] == 1;

            // Branch 2: detonate (0=hold = don't detonate, 1=release = detonate)
            LastDetonateHeld = discrete[2] == 0;

            // Fix 2: gate behind verboseLogging (default false) to avoid string alloc every 1.5s per agent
            if (verboseLogging && Time.time - _lastOnActionLogTime >= 1.5f)
            {
                _lastOnActionLogTime = Time.time;
                Debug.Log($"[BombermanAgent] {gameObject.name} OnActionReceived moveAction={moveAction} LastMove={LastMove}");
            }
        }

        private float _lastHeuristicLogTime = -999f;

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discrete = actionsOut.DiscreteActions;
            if (discrete.Length < 3) return;

            Vector2 myPos = transform.position;
            Vector2 cell = new Vector2(Mathf.Round(myPos.x), Mathf.Round(myPos.y));
            GameObject[] allPlayers = _localGameManager?.GetPlayers();
            var destructible = _bombController != null ? _bombController.destructibleTiles : null;

            // Safety first: flee bomb blast zones AND active explosion fire
            if (IsCellInDanger(cell) || IsCellUnderExplosion(cell))
            {
                Vector2 safe = FindEscapeDirection(cell, destructible);
                discrete[0] = DirToAction(safe);
                discrete[1] = 0;
                discrete[2] = 0;
                return;
            }

            // Fix 4: compute safe directions once and reuse across all branches below
            var safeDirs = GetSafeDirections(cell, destructible);

            GameObject nearest = GetNearestOther(allPlayers);

            // Opponent-mode bomb placement — require BFS escape route before committing
            if (nearest != null && _bombController != null && _bombController.bombAmount > 0)
            {
                float dist = Vector2.Distance(myPos, nearest.transform.position);
                bool canEscape = HasEscapeFromBomb(cell, _bombController.explosionRadius, destructible);
                if (dist <= 2.5f && canEscape)
                {
                    discrete[0] = safeDirs.Count > 0
                        ? DirToAction(safeDirs[0])
                        : DirToAction(FindEscapeDirection(cell, destructible));
                    discrete[1] = 1;
                    discrete[2] = 0;
                    return;
                }
            }

            if (nearest != null)
            {
                // Chase opponent — only move in that direction if the destination cell is safe
                Vector2 toTarget = ((Vector2)nearest.transform.position - myPos).normalized;
                Vector2 preferred = ActionToDir(DirToAction(toTarget));
                discrete[0] = safeDirs.Contains(preferred)
                    ? DirToAction(preferred)
                    : (safeDirs.Count > 0
                        ? DirToAction(safeDirs[Random.Range(0, safeDirs.Count)])
                        : DirToAction(FindEscapeDirection(cell, destructible)));
                discrete[1] = 0;
            }
            else
            {
                // Solo mode — roam toward destructible blocks, only bomb when BFS escape exists AND a destructible tile is in range
                bool canEscape = _bombController != null && _bombController.bombAmount > 0
                    && HasEscapeFromBomb(cell, _bombController.explosionRadius, destructible);
                bool nearDestructible = canEscape && IsDestructibleInBlastRange(cell,
                    _bombController != null ? _bombController.explosionRadius : 1, destructible);

                discrete[1] = nearDestructible ? 1 : 0;

                if (safeDirs.Count > 0)
                {
                    // Prefer a direction that moves toward a destructible tile
                    Vector2 chosen = safeDirs[Random.Range(0, safeDirs.Count)];
                    foreach (var d in safeDirs)
                    {
                        Vector2 neighbour = cell + d;
                        if (destructible != null && destructible.GetTile(destructible.WorldToCell(neighbour)) != null)
                        {
                            chosen = d;
                            break;
                        }
                    }
                    discrete[0] = DirToAction(chosen);
                }
                else
                {
                    // All adjacent cells are dangerous — pick least-bad direction, never bomb
                    discrete[0] = DirToAction(FindEscapeDirection(cell, destructible));
                    discrete[1] = 0;
                }
            }

            discrete[2] = 0;

            // Fix 2: gate behind verboseLogging to avoid string alloc every 1.5s per agent
            if (verboseLogging && Time.time - _lastHeuristicLogTime >= 1.5f)
            {
                _lastHeuristicLogTime = Time.time;
                Vector2 logMove = discrete[0] switch { 1 => Vector2.up, 2 => Vector2.down, 3 => Vector2.left, 4 => Vector2.right, _ => Vector2.zero };
                Debug.Log($"[BombermanAgent] {gameObject.name} Heuristic ran → moveAction={discrete[0]} move={logMove}");
            }
        }

        private void FixedUpdate()
        {
            if (!IsActive()) return;

            AddReward(rewardPerStep);

            int opponents = GetActiveOpponentCount();
            if (opponents < _opponentCountLastStep)
                AddReward(rewardKillOpponent);
            _opponentCountLastStep = opponents;

            Vector2 stepCell = new Vector2(Mathf.Round(((Vector2)transform.position).x),
                                           Mathf.Round(((Vector2)transform.position).y));
            if (IsCellInDanger(stepCell) || IsCellUnderExplosion(stepCell))
                AddReward(rewardInDangerPerStep);
        }

        private bool IsActive()
        {
            return gameObject.activeInHierarchy && _playerController != null;
        }

        public void NotifyDeath()
        {
            AddReward(rewardDeath);
            try
            {
                EndEpisode();
            }
            catch (System.NullReferenceException)
            {
                // UpdateSensors() can throw when agent is already disabled or sensors not initialized (e.g. death from shrink).
            }
        }

        public void NotifyCollectedItem()  { AddReward(rewardCollectItem); }
        public void NotifyPlacedBomb()     { AddReward(rewardPlaceBomb); }
        public void NotifyDestroyedBlock() { AddReward(rewardDestroyBlock); }

        private void CountOpponents()
        {
            _opponentCountLastStep = GetActiveOpponentCount();
        }

        private int GetActiveOpponentCount()
        {
            var gm = _localGameManager;
            if (gm == null) return 0;
            int count = 0;
            foreach (var p in gm.GetPlayers())
            {
                if (p != null && p.activeInHierarchy && p.transform != transform)
                    count++;
            }
            return count;
        }

        private GameObject GetNearestOther(GameObject[] allPlayers)
        {
            if (allPlayers == null) return null;
            GameObject nearest = null;
            float best = 999f;
            foreach (var p in allPlayers)
            {
                if (p == null || !p.activeInHierarchy || p.transform == transform) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = p; }
            }
            return nearest;
        }

        private bool IsCellInDanger(Vector2 cell)
        {
            var destr = _bombController?.destructibleTiles;
            var indestr = _bombController?.indestructibleTiles;
            foreach (var b in GetArenaBombs())
            {
                if (b == null) continue;
                Vector2 bc = new Vector2(Mathf.Round(b.transform.position.x), Mathf.Round(b.transform.position.y));
                if (cell == bc) return true;
                // Fix 1: Dirs4 replaces inline new[] allocation
                foreach (var d in Dirs4)
                {
                    for (int i = 1; i <= b.explosionRadius; i++)
                    {
                        Vector2 check = bc + d * i;
                        if (!IsWalkable(check, destr, indestr)) break; // wall blocks blast here and beyond
                        if (check == cell) return true;
                    }
                }
            }
            return false;
        }

        private bool IsInSingleBombBlast(Vector2 cell, Vector2 bombCell, int radius,
            UnityEngine.Tilemaps.Tilemap destr, UnityEngine.Tilemaps.Tilemap indestr)
        {
            if (cell == bombCell) return true;
            // Fix 1: Dirs4 replaces inline new[] allocation
            foreach (var d in Dirs4)
            {
                for (int i = 1; i <= radius; i++)
                {
                    Vector2 check = bombCell + d * i;
                    if (!IsWalkable(check, destr, indestr)) break;
                    if (check == cell) return true;
                }
            }
            return false;
        }

        private Vector2 FindEscapeDirection(Vector2 start, UnityEngine.Tilemaps.Tilemap destr)
        {
            var indestr = _bombController?.indestructibleTiles;
            // Fix 3: clear and reuse pooled BFS collections instead of allocating new ones
            _bfsQueue.Clear(); _bfsVisited.Clear(); _bfsParent.Clear();
            _bfsQueue.Enqueue(start); _bfsVisited.Add(start);
            while (_bfsQueue.Count > 0)
            {
                var cur = _bfsQueue.Dequeue();
                if (cur != start && !IsCellInDanger(cur) && !IsCellUnderExplosion(cur))
                {
                    // Trace parent chain back to the first step from start
                    var step = cur;
                    while (_bfsParent[step] != start) step = _bfsParent[step];
                    return step - start;
                }
                // Fix 1: Dirs4 replaces local dirs array
                foreach (var d in Dirs4)
                {
                    var next = cur + d;
                    if (_bfsVisited.Contains(next) || !IsWalkable(next, destr, indestr)) continue;
                    _bfsVisited.Add(next); _bfsParent[next] = cur; _bfsQueue.Enqueue(next);
                }
            }
            // No safe cell reachable — fall back to least-dangerous neighbour
            return FindSafestNeighbour(start, destr);
        }

        private Vector2 FindSafestNeighbour(Vector2 myCell, UnityEngine.Tilemaps.Tilemap destructible)
        {
            var indestructible = _bombController != null ? _bombController.indestructibleTiles : null;
            Vector2 best = Vector2.zero;
            int bestDanger = int.MaxValue;
            // Fix 1: Dirs4 replaces local dirs array
            foreach (var d in Dirs4)
            {
                Vector2 next = myCell + d;
                if (!IsWalkable(next, destructible, indestructible)) continue;
                int danger = CountDanger(next);
                if (danger < bestDanger) { bestDanger = danger; best = d; }
            }
            return best;
        }

        private bool HasEscapeFromBomb(Vector2 bombCell, int radius, UnityEngine.Tilemaps.Tilemap destr)
        {
            var indestr = _bombController?.indestructibleTiles;
            // Fix 3: clear and reuse pooled BFS collections instead of allocating new ones
            _bfsQueue.Clear(); _bfsVisited.Clear();
            _bfsQueue.Enqueue(bombCell); _bfsVisited.Add(bombCell);
            while (_bfsQueue.Count > 0)
            {
                var cur = _bfsQueue.Dequeue();
                if (!IsInSingleBombBlast(cur, bombCell, radius, destr, indestr) && !IsCellInDanger(cur))
                    return true;
                // Fix 1: Dirs4 replaces local dirs array
                foreach (var d in Dirs4)
                {
                    var next = cur + d;
                    if (_bfsVisited.Contains(next) || !IsWalkable(next, destr, indestr)) continue;
                    _bfsVisited.Add(next); _bfsQueue.Enqueue(next);
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if at least one destructible tile falls within the bomb's blast range
        /// from <paramref name="bombCell"/>, stopping at indestructible walls.
        /// </summary>
        private bool IsDestructibleInBlastRange(Vector2 bombCell, int radius,
            UnityEngine.Tilemaps.Tilemap destr)
        {
            if (destr == null) return false;
            var indestr = _bombController?.indestructibleTiles;
            foreach (var d in Dirs4)
            {
                for (int i = 1; i <= radius; i++)
                {
                    Vector2 check = bombCell + d * i;
                    if (indestr != null && indestr.GetTile(indestr.WorldToCell(check)) != null)
                        break;
                    if (destr.GetTile(destr.WorldToCell(check)) != null)
                        return true;
                }
            }
            return false;
        }

        private int CountDanger(Vector2 cell)
        {
            int c = 0;
            var destr = _bombController?.destructibleTiles;
            var indestr = _bombController?.indestructibleTiles;
            foreach (var b in GetArenaBombs())
            {
                if (b == null) continue;
                Vector2 bc = new Vector2(Mathf.Round(b.transform.position.x), Mathf.Round(b.transform.position.y));
                if (cell == bc) { c++; continue; }
                // Fix 1: Dirs4 replaces inline new[] allocation
                foreach (var d in Dirs4)
                {
                    for (int i = 1; i <= b.explosionRadius; i++)
                    {
                        Vector2 check = bc + d * i;
                        if (!IsWalkable(check, destr, indestr)) break;
                        if (check == cell) { c++; break; }
                    }
                }
            }
            // Active explosion fire is immediate — weight it heavily so FindSafestNeighbour avoids it
            if (IsCellUnderExplosion(cell)) c += 10;
            return c;
        }

        private bool IsCellUnderExplosion(Vector2 cell)
        {
            var explosions = GetArenaExplosions();
            foreach (var e in explosions)
            {
                if (e == null) continue;
                Vector2 ec = new Vector2(Mathf.Round(e.transform.position.x), Mathf.Round(e.transform.position.y));
                if (ec == cell) return true;
            }
            return false;
        }

        private static bool IsWalkable(Vector2 cell, UnityEngine.Tilemaps.Tilemap destructible, UnityEngine.Tilemaps.Tilemap indestructible = null)
        {
            if (destructible != null && destructible.GetTile(destructible.WorldToCell(cell)) != null)
                return false;
            if (indestructible != null && indestructible.GetTile(indestructible.WorldToCell(cell)) != null)
                return false;
            return true;
        }

        private ItemPickup GetNearestItem(Vector2 myPos)
        {
            var items = GetArenaItems();
            ItemPickup nearest = null;
            float best = 400f;
            foreach (var item in items)
            {
                if (item == null) continue;
                float d = ((Vector2)item.transform.position - myPos).sqrMagnitude;
                if (d < best) { best = d; nearest = item; }
            }
            return nearest;
        }

        private static int DirToAction(Vector2 d)
        {
            if (d.y > 0.3f) return 1;
            if (d.y < -0.3f) return 2;
            if (d.x < -0.3f) return 3;
            if (d.x > 0.3f) return 4;
            return 0;
        }

        private static Vector2 ActionToDir(int action) => action switch
        {
            1 => Vector2.up,
            2 => Vector2.down,
            3 => Vector2.left,
            4 => Vector2.right,
            _ => Vector2.zero
        };

        // Fix 4: fills and returns the pre-allocated _safeDirs list instead of allocating a new List each call
        private List<Vector2> GetSafeDirections(Vector2 cell, UnityEngine.Tilemaps.Tilemap destructible)
        {
            var indestructible = _bombController != null ? _bombController.indestructibleTiles : null;
            _safeDirs.Clear();
            // Fix 1: Dirs4 replaces local dirs array
            foreach (var d in Dirs4)
            {
                Vector2 next = cell + d;
                if (IsWalkable(next, destructible, indestructible) && !IsCellInDanger(next) && !IsCellUnderExplosion(next))
                    _safeDirs.Add(d);
            }
            return _safeDirs;
        }

        public static int GetVectorObservationSize() => ObservationSize;
        public static int[] GetDiscreteBranchSizes() => new[] { 5, 2, 2 };
    }
}
