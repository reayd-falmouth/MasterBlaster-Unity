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
        public float rewardPerStep = 0.001f;
        public float rewardKillOpponent = 1f;
        public float rewardCollectItem = 0.3f;
        public float rewardDeath = -1f;

        private const int ObservationSize = 15;
        private const float ArenaScale = 15f;

        // Last action output for MLAgentsBrain to read
        public Vector2 LastMove { get; private set; }
        public bool LastPlaceBomb { get; private set; }
        public bool LastDetonateHeld { get; private set; } = true;

        private int _opponentCountLastStep;
        private BombController _bombController;
        private PlayerController _playerController;

        private void Awake()
        {
            _bombController = GetComponent<BombController>();
            _playerController = GetComponent<PlayerController>();
            EnsureBehaviorParameters();
        }

        private void EnsureBehaviorParameters()
        {
            var bp = GetComponent<BehaviorParameters>();
            if (bp == null)
            {
                bp = gameObject.AddComponent<BehaviorParameters>();
                bp.BehaviorName = "Bomberman";
                bp.TeamId = 0;
                bp.BrainParameters.VectorObservationSize = ObservationSize;
                bp.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 5, 2, 2 });
            }
        }

        private void OnEnable()
        {
            CountOpponents();
            _opponentCountLastStep = GetActiveOpponentCount();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (sensor == null) return;

            Vector2 myPos = transform.position;
            var gm = GameManager.Instance;
            GameObject[] allPlayers = gm != null ? gm.GetPlayers() : null;

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

            // 5-7: nearest bomb relative + radius
            var (bombPos, radius) = GetNearestBomb(myPos);
            if (bombPos.HasValue)
            {
                Vector2 delta = bombPos.Value - myPos;
                sensor.AddObservation(Mathf.Clamp(delta.x / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(delta.y / ArenaScale, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp01(radius / 5f));
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }

            // 8: is my cell in danger (0 or 1)
            Vector2 cell = new Vector2(Mathf.Round(myPos.x), Mathf.Round(myPos.y));
            sensor.AddObservation(IsCellInDanger(cell) ? 1f : 0f);

            // 9-10: own stats normalized
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

            // 11-12: nearest item relative (if any)
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

            // 13-14: padding to fixed size (for optional extra features later)
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

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
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discrete = actionsOut.DiscreteActions;
            if (discrete.Length < 3) return;

            Vector2 myPos = transform.position;
            Vector2 cell = new Vector2(Mathf.Round(myPos.x), Mathf.Round(myPos.y));
            var gm = GameManager.Instance;
            GameObject[] allPlayers = gm != null ? gm.GetPlayers() : null;
            var destructible = _bombController != null ? _bombController.destructibleTiles : null;

            // Safety first
            if (IsCellInDanger(cell))
            {
                Vector2 safe = FindSafestDirection(cell, destructible);
                discrete[0] = DirToAction(safe);
                discrete[1] = 0;
                discrete[2] = 0;
                return;
            }

            GameObject nearest = GetNearestOther(allPlayers);
            if (nearest != null && _bombController != null && _bombController.bombAmount > 0)
            {
                float dist = Vector2.Distance(myPos, nearest.transform.position);
                if (dist <= 2.5f && IsWalkable(cell, destructible))
                {
                    discrete[0] = 0;
                    discrete[1] = 1;
                    discrete[2] = 0;
                    return;
                }
            }

            if (nearest != null)
            {
                Vector2 toTarget = ((Vector2)nearest.transform.position - myPos).normalized;
                discrete[0] = DirToAction(toTarget);
            }
            else
            {
                discrete[0] = Random.Range(0, 5);
            }

            discrete[1] = 0;
            discrete[2] = 0;
        }

        private void FixedUpdate()
        {
            if (!IsActive()) return;

            AddReward(rewardPerStep);

            int opponents = GetActiveOpponentCount();
            if (opponents < _opponentCountLastStep)
                AddReward(rewardKillOpponent);
            _opponentCountLastStep = opponents;
        }

        private bool IsActive()
        {
            return gameObject.activeInHierarchy && _playerController != null;
        }

        public void NotifyDeath()
        {
            AddReward(rewardDeath);
            EndEpisode();
        }

        public void NotifyCollectedItem()
        {
            AddReward(rewardCollectItem);
        }

        private void CountOpponents()
        {
            _opponentCountLastStep = GetActiveOpponentCount();
        }

        private int GetActiveOpponentCount()
        {
            var gm = GameManager.Instance;
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

        private (Vector2? position, int radius) GetNearestBomb(Vector2 myPos)
        {
            var bombs = FindObjectsByType<BombInfo>(FindObjectsSortMode.None);
            Vector2? nearest = null;
            int r = 0;
            float best = 999f;
            foreach (var b in bombs)
            {
                if (b == null) continue;
                float d = ((Vector2)b.transform.position - myPos).sqrMagnitude;
                if (d < best) { best = d; nearest = b.transform.position; r = b.explosionRadius; }
            }
            return (nearest, r);
        }

        private bool IsCellInDanger(Vector2 cell)
        {
            var bombs = FindObjectsByType<BombInfo>(FindObjectsSortMode.None);
            foreach (var b in bombs)
            {
                if (b == null) continue;
                Vector2 bc = new Vector2(Mathf.Round(b.transform.position.x), Mathf.Round(b.transform.position.y));
                int r = b.explosionRadius;
                if (cell.x == bc.x && Mathf.Abs(cell.y - bc.y) <= r) return true;
                if (cell.y == bc.y && Mathf.Abs(cell.x - bc.x) <= r) return true;
            }
            return false;
        }

        private Vector2 FindSafestDirection(Vector2 myCell, UnityEngine.Tilemaps.Tilemap destructible)
        {
            Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            Vector2 best = Vector2.zero;
            int bestDanger = int.MaxValue;
            foreach (var d in dirs)
            {
                Vector2 next = myCell + d;
                if (!IsWalkable(next, destructible)) continue;
                int danger = CountDanger(next);
                if (danger < bestDanger) { bestDanger = danger; best = d; }
            }
            return best;
        }

        private int CountDanger(Vector2 cell)
        {
            int c = 0;
            var bombs = FindObjectsByType<BombInfo>(FindObjectsSortMode.None);
            foreach (var b in bombs)
            {
                if (b == null) continue;
                Vector2 bc = new Vector2(Mathf.Round(b.transform.position.x), Mathf.Round(b.transform.position.y));
                int r = b.explosionRadius;
                if (cell.x == bc.x && Mathf.Abs(cell.y - bc.y) <= r) c++;
                if (cell.y == bc.y && Mathf.Abs(cell.x - bc.x) <= r) c++;
            }
            return c;
        }

        private static bool IsWalkable(Vector2 cell, UnityEngine.Tilemaps.Tilemap destructible)
        {
            if (destructible != null && destructible.GetTile(destructible.WorldToCell(cell)) != null)
                return false;
            return true;
        }

        private ItemPickup GetNearestItem(Vector2 myPos)
        {
            var items = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
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

        public static int GetVectorObservationSize() => ObservationSize;
        public static int[] GetDiscreteBranchSizes() => new[] { 5, 2, 2 };
    }
}
