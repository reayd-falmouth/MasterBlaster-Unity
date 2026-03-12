using System.Collections;
using System.Collections.Generic;
using Core;
using Scenes.Arena.Map;
using Scenes.Arena.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Scenes.Arena.Bomb
{
    public class BombController : NetworkBehaviour
    {
        [Header("Bomb")]
        public KeyCode inputKey = KeyCode.LeftShift;
        public GameObject bombPrefab;
        public float bombFuseTime = 3f;
        public int bombAmount = 1;
        private int bombsRemaining;
        public bool timeBomb = false;
        public bool remoteBomb = false;

        [Header("Explosion")]
        public Explosion explosionPrefab;
        public LayerMask explosionLayerMask;
        public float explosionDuration = 0.5f;
        public int explosionRadius = 1;
        public float explosionDelay = 0.05f;

        [Header("Destructible")]
        public Tilemap destructibleTiles;
        public Destructible destructiblePrefab;

        [Header("Indestructible (auto-detected)")]
        [HideInInspector] public Tilemap indestructibleTiles;

        // Fix 8: HashSet gives O(1) Add/Remove/Contains vs O(n) List.Remove
        private readonly HashSet<GameObject> activeBombs = new HashSet<GameObject>();

        // Fix 5: pre-allocated overlap buffers — two separate buffers so ExplodeBomb center-overlap
        // doesn't overwrite the buffer mid-iteration in ExplodeCoroutine during chain reactions
        private static readonly Collider2D[] _explodeBuffer = new Collider2D[16];
        private static readonly Collider2D[] _centerBuffer  = new Collider2D[16];

        private int baseBombAmount;
        private int baseExplosionRadius;
        private Player.IPlayerInput _inputProvider;

        // Fix 6: bool flag avoids per-frame destroyed-object cast (is UnityEngine.Object uo && uo == null)
        private bool _inputProviderValid;

        // Fix 7: cached component references — set once in Awake, avoids GetComponent per bomb-place/destroy
        private PlayerController _playerController;
        private Scenes.Arena.Player.AI.BombermanAgent _agentNotify;

        // Non-null when the player sits under a shared arena root (multi-arena training).
        // Bombs and explosions are parented here so BombermanAgent scoped searches work correctly.
        private Transform _arenaRoot;

        // Always-active MonoBehaviour used to host coroutines. The player GameObject may be
        // inactive when a bomb detonates (player died before fuse expired), so we must not call
        // StartCoroutine on 'this' in that case. The arena's GameManager is always active.
        private MonoBehaviour _coroutineRunner;

        private void Awake()
        {
            baseBombAmount = bombAmount;
            baseExplosionRadius = explosionRadius;

            // Arena root: use the hierarchy root only when this object is NOT already the root.
            _arenaRoot = transform.root != transform ? transform.root : null;

            // Cache an always-active coroutine runner.
            var gm = (_arenaRoot != null
                ? _arenaRoot.GetComponentInChildren<GameManager>()
                : null) ?? GameManager.Instance;
            _coroutineRunner = (gm as MonoBehaviour) ?? this;

            // Fix 7: cache both references once
            _playerController = GetComponent<PlayerController>();
            _agentNotify      = GetComponent<Scenes.Arena.Player.AI.BombermanAgent>();

            if (!indestructibleTiles)
            {
                var tilemaps = _arenaRoot != null
                    ? _arenaRoot.GetComponentsInChildren<Tilemap>()
                    : FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                foreach (var t in tilemaps)
                    if (t.name == "Indestructibles") { indestructibleTiles = t; break; }
            }
        }

        private void Start()
        {
            // Fix 6: set valid flag so Update skips GetComponent in steady state
            _inputProvider = GetComponent<Player.IPlayerInput>();
            _inputProviderValid = _inputProvider != null;
        }

        private void Update()
        {
            // Re-resolve if null or if the reference points to a destroyed component
            // (mirrors PlayerController.Update; needed when AttachInputProvider Destroy+re-adds AIPlayerInput)
            if (!_inputProviderValid || (_inputProvider is UnityEngine.Object uoInput && uoInput == null))
            {
                _inputProvider = GetComponent<Player.IPlayerInput>();
                _inputProviderValid = _inputProvider != null;
            }

            if (bombsRemaining <= 0)
                return;

            // In online play, only the host places bombs.
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && !IsServer)
                return;

            bool wantBomb = _inputProvider != null ? _inputProvider.GetBombDown() : Input.GetKeyDown(inputKey);
            if (wantBomb)
            {
                GameObject bomb = SpawnBomb();
                if (bomb == null)
                    return;

                var brain = bomb.GetComponent<RemoteBombController>();

                var mode = remoteBomb
                    ? RemoteBombController.BombMode.Remote
                    : (
                        timeBomb
                            ? RemoteBombController.BombMode.Time
                            : RemoteBombController.BombMode.Fuse
                    );

                // Fix 7: use cached _playerController instead of GetComponent per bomb-place
                brain.Init(_playerController, this, mode, inputKey, bombFuseTime);
            }
        }

        /// <summary>
        /// Spawns a bomb at the player's grid cell and returns it.
        /// Handles destructible blocking and duplicate checks.
        /// </summary>
        private GameObject SpawnBomb()
        {
            Vector2 position = transform.position;
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);

            Vector3Int cell = destructibleTiles.WorldToCell(position);
            if (destructibleTiles.GetTile(cell) != null)
                return null;

            // Fix 8: HashSet enumeration still works; position check is now O(1) per entry
            foreach (var b in activeBombs)
                if (b != null && (Vector2)b.transform.position == position)
                    return null;

            GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity, _arenaRoot);
            var info = bomb.AddComponent<BombInfo>();
            info.explosionRadius = explosionRadius;
            activeBombs.Add(bomb);
            bombsRemaining--;

            // Fix 7: use cached _agentNotify instead of GetComponent per bomb-place
            _agentNotify?.NotifyPlacedBomb();

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && IsServer)
                SpawnBombClientRpc(position, explosionRadius);

            return bomb;
        }

        public void Explode(
            Vector2 position,
            Vector2 direction,
            int length,
            float delayStep = 0.05f
        )
        {
            _coroutineRunner.StartCoroutine(ExplodeCoroutine(position, direction, length, delayStep));
        }

        private IEnumerator ExplodeCoroutine(
            Vector2 position,
            Vector2 direction,
            int length,
            float delayStep
        )
        {
            for (int i = 1; i <= length; i++)
            {
                Vector2 nextPos = position + direction * i;

                // Fix 5: NonAlloc reuses _explodeBuffer; separate buffer from _centerBuffer so
                // ExplodeBomb (called synchronously below) doesn't overwrite our iteration buffer
                int count = Physics2D.OverlapBoxNonAlloc(nextPos, Vector2.one / 2f, 0f, _explodeBuffer);

                bool blocked = false;

                for (int j = 0; j < count; j++)
                {
                    var hit = _explodeBuffer[j];
                    if (hit == null)
                        continue;

                    // Player in explosion cell – apply damage, explosion keeps going
                    var pc = hit.GetComponent<PlayerController>();
                    if (pc != null && pc.enabled)
                    {
                        pc.TryApplyExplosionDamage();
                        continue;
                    }

                    // Pushable destructible prefab
                    var destructible = hit.GetComponent<Destructible>();
                    if (destructible != null)
                    {
                        destructible.DestroyBlock();
                        blocked = true; // stop propagation
                        break;
                    }

                    // Item
                    var item = hit.GetComponent<ItemPickup>();
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                        continue; // explosion keeps going
                    }

                    // Bomb chain reaction (⚡ doesn't block)
                    if (hit.gameObject.layer == LayerMask.NameToLayer("Bomb"))
                    {
                        ExplodeBomb(hit.gameObject);
                        continue;
                    }

                    // Tilemap destructible / wall
                    if (((1 << hit.gameObject.layer) & explosionLayerMask) != 0)
                    {
                        ClearDestructible(nextPos);
                        blocked = true; // stop propagation
                        break;
                    }
                }

                // stop propagation if blocked by destructible/wall
                if (blocked)
                    yield break;

                // Spawn explosion fire
                Explosion explosion = Instantiate(explosionPrefab, nextPos, Quaternion.identity, _arenaRoot);
                explosion.SetDirection(direction);
                explosion.DestroyAfter(explosionDuration);

                yield return new WaitForSeconds(delayStep);
            }
        }

        public void ExplodeBomb(GameObject bomb)
        {
            if (bomb == null)
                return;

            Vector2 position = bomb.transform.position;
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);

            // Fix 8: HashSet.Remove is O(1)
            activeBombs.Remove(bomb);
            Destroy(bomb);
            bombsRemaining++;

            // Center fire – play explosion sound on this instance only
            Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity, _arenaRoot);
            explosion.PlayExplosionSound();
            explosion.DestroyAfter(explosionDuration);

            // Fix 5: use _centerBuffer (separate from _explodeBuffer) so chain-reaction calls
            // from within ExplodeCoroutine don't overwrite the coroutine's iteration buffer
            int centerCount = Physics2D.OverlapBoxNonAlloc(position, Vector2.one / 2f, 0f, _centerBuffer);
            for (int i = 0; i < centerCount; i++)
            {
                var hit = _centerBuffer[i];
                if (hit == null) continue;
                var pc = hit.GetComponent<PlayerController>();
                if (pc != null && pc.enabled)
                    pc.TryApplyExplosionDamage();
            }

            // Defer propagation to next frame to avoid stack overflow when two bombs chain.
            // Use _coroutineRunner (the arena's GameManager) so this works even when the player
            // is inactive (e.g. player died before the fuse expired).
            _coroutineRunner.StartCoroutine(ExplodeBombPropagateNextFrame(position));
        }

        private IEnumerator ExplodeBombPropagateNextFrame(Vector2 position)
        {
            yield return null;

            Explode(position, Vector2.up, explosionRadius, explosionDelay);
            Explode(position, Vector2.down, explosionRadius, explosionDelay);
            Explode(position, Vector2.left, explosionRadius, explosionDelay);
            Explode(position, Vector2.right, explosionRadius, explosionDelay);
        }

        private void ClearDestructible(Vector2 position)
        {
            Vector3Int cell = destructibleTiles.WorldToCell(position);
            TileBase tile = destructibleTiles.GetTile(cell);

            if (tile != null)
            {
                var debris = Instantiate(destructiblePrefab, position, Quaternion.identity);
                var destructible = debris.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.isDebris = true; // auto-destroy after destructionTime
                }

                destructibleTiles.SetTile(cell, null);
                // Fix 7: use cached _agentNotify instead of GetComponent per block-destroy
                _agentNotify?.NotifyDestroyedBlock();

                bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
                if (isOnline && IsServer)
                    ClearDestructibleClientRpc(cell);
            }
        }

        // ── Online ClientRpcs ───────────────────────────────────────────────────────

        /// <summary>Tells clients to clear the destructible tile at <paramref name="cell"/>.</summary>
        [ClientRpc]
        private void ClearDestructibleClientRpc(Vector3Int cell)
        {
            // Already cleared on host; clients need to update their local tilemap.
            if (IsServer) return;
            if (destructibleTiles != null)
                destructibleTiles.SetTile(cell, null);
        }

        /// <summary>Tells clients to show an explosion visual segment.</summary>
        [ClientRpc]
        public void ShowExplosionClientRpc(Vector2 pos, Vector2 dir, int length)
        {
            if (IsServer) return;
            Explode(pos, dir, length, explosionDelay);
        }

        /// <summary>Tells clients to spawn a visual-only bomb at <paramref name="pos"/>.</summary>
        [ClientRpc]
        public void SpawnBombClientRpc(Vector2 pos, int radius)
        {
            if (IsServer) return;
            // Spawn a visual-only bomb (no fuse logic; host drives detonation).
            if (bombPrefab != null)
            {
                var visual = Instantiate(bombPrefab, pos, Quaternion.identity, _arenaRoot);
                // Disable the RemoteBombController so it doesn't self-detonate on clients.
                var rbc = visual.GetComponent<RemoteBombController>();
                if (rbc != null) rbc.enabled = false;
            }
        }

        public void AddBomb()
        {
            bombAmount++;
            bombsRemaining++;
        }

        public void IncreaseBlastRadius()
        {
            explosionRadius++;
        }

        public void EnableTimeBomb()
        {
            timeBomb = true;
            remoteBomb = false;
        }

        public void EnableRemoteBomb()
        {
            remoteBomb = true;
            timeBomb = false;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Bomb"))
            {
                other.isTrigger = false;
            }
        }

        private void OnEnable()
        {
            activeBombs.Clear();
            bombsRemaining = bombAmount;

            // Fix 7: use cached _playerController instead of GetComponent
            if (_playerController != null)
            {
                ApplyUpgrades(_playerController.playerId);
            }
        }

        public void ApplyUpgrades(int playerId)
        {
            if (Core.SessionManager.Instance == null)
                return;
            // Capture base from current values if Awake hasn't run yet (e.g. in EditMode tests)
            if (baseBombAmount == 0)
                baseBombAmount = bombAmount;
            if (baseExplosionRadius == 0)
                baseExplosionRadius = explosionRadius;
            // Reset to base values so multiple calls (e.g. OnEnable + GameManager) are idempotent
            bombAmount = baseBombAmount;
            explosionRadius = baseExplosionRadius;
            timeBomb = false;
            remoteBomb = false;

            // Extra bombs
            int extraBombs = Core.SessionManager.Instance.GetUpgradeLevel(
                playerId,
                Scenes.Shop.ShopItemType.ExtraBomb
            );
            bombAmount += extraBombs;
            bombsRemaining = bombAmount;

            // Blast radius
            int powerUps = Core.SessionManager.Instance.GetUpgradeLevel(
                playerId,
                Scenes.Shop.ShopItemType.PowerUp
            );
            explosionRadius += powerUps;

            // Timebomb toggle
            if (
                Core.SessionManager.Instance.GetUpgradeLevel(
                    playerId,
                    Scenes.Shop.ShopItemType.Timebomb
                ) == 1
            )
                timeBomb = true;

            // Remote bomb toggle
            if (
                Core.SessionManager.Instance.GetUpgradeLevel(
                    playerId,
                    Scenes.Shop.ShopItemType.Controller
                ) == 1
            )
                remoteBomb = true;

            // Debug.Log(
            //     $"[BombController] Player {playerId} upgrades applied: bombs={bombAmount}, radius={explosionRadius}, timeBomb={timeBomb}, remoteBomb={remoteBomb}"
            // );
        }
    }
}
