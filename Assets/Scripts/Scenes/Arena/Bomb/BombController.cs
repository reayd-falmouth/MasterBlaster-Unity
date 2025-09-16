using System.Collections;
using System.Collections.Generic;
using Core;
using Scenes.Arena.Map;
using Scenes.Arena.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Scenes.Arena.Bomb
{
    public class BombController : MonoBehaviour
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

        private readonly List<GameObject> activeBombs = new List<GameObject>();
        
        private void Update()
        {
            if (bombsRemaining <= 0) return;

            if (Input.GetKeyDown(inputKey))
            {
                GameObject bomb = SpawnBomb();
                if (bomb == null) return;

                var player = GetComponent<PlayerController>();
                var brain  = bomb.GetComponent<RemoteBombController>();

                var mode = remoteBomb ? RemoteBombController.BombMode.Remote
                    : (timeBomb ? RemoteBombController.BombMode.Time
                        : RemoteBombController.BombMode.Fuse);

                brain.Init(player, this, mode, inputKey, bombFuseTime);
            }
        }
        
        /// <summary>
        /// Spawns a bomb at the player’s grid cell and returns it.
        /// Handles destructible blocking and duplicate checks.
        /// </summary>
        private GameObject SpawnBomb()
        {
            Vector2 position = transform.position;
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);

            Vector3Int cell = destructibleTiles.WorldToCell(position);
            if (destructibleTiles.GetTile(cell) != null) return null;

            foreach (var b in activeBombs)
                if (b != null && (Vector2)b.transform.position == position)
                    return null;

            GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);
            activeBombs.Add(bomb);
            bombsRemaining--;

            return bomb;
        }
        
        public void Explode(Vector2 position, Vector2 direction, int length, float delayStep = 0.05f)
        {
            StartCoroutine(ExplodeCoroutine(position, direction, length, delayStep));
        }
        
        private IEnumerator ExplodeCoroutine(Vector2 position, Vector2 direction, int length, float delayStep)
        {
            for (int i = 1; i <= length; i++)
            {
                Vector2 nextPos = position + direction * i;

                // Grab everything in this cell
                var hits = Physics2D.OverlapBoxAll(nextPos, Vector2.one / 2f, 0f);

                bool blocked = false;

                foreach (var hit in hits)
                {
                    if (hit == null) continue;

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
                if (blocked) yield break;

                // Spawn explosion fire
                Explosion explosion = Instantiate(explosionPrefab, nextPos, Quaternion.identity);
                explosion.SetDirection(direction);
                explosion.DestroyAfter(explosionDuration);

                yield return new WaitForSeconds(delayStep);
            }
        }

        public void ExplodeBomb(GameObject bomb)
        {
            if (bomb == null) return;

            AudioController.I?.PlayExplosion();

            Vector2 position = bomb.transform.position;
            position.x = Mathf.Round(position.x);
            position.y = Mathf.Round(position.y);

            // center fire
            Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
            explosion.DestroyAfter(explosionDuration);

            // propagate
            Explode(position, Vector2.up, explosionRadius, explosionDelay);
            Explode(position, Vector2.down, explosionRadius, explosionDelay);
            Explode(position, Vector2.left, explosionRadius, explosionDelay);
            Explode(position, Vector2.right, explosionRadius, explosionDelay);

            activeBombs.Remove(bomb);
            Destroy(bomb);
            bombsRemaining++;
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
            if (other.gameObject.layer == LayerMask.NameToLayer("Bomb")) {
                other.isTrigger = false;
            }
        }
        
        private void OnEnable()
        {
            bombsRemaining = bombAmount;

            // Load upgrades from PlayerPrefs
            var pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                ApplyUpgrades(pc.playerId);
            }
        }

        public void ApplyUpgrades(int playerId)
        {
            // Reset to base values first
            bombAmount = 1;
            bombsRemaining = bombAmount;
            explosionRadius = 1;
            timeBomb = false;
            remoteBomb = false;

            // Extra bombs
            int extraBombs = PlayerPrefs.GetInt($"Player{playerId}_{Scenes.Shop.ShopItemType.ExtraBomb}", 0);
            bombAmount += extraBombs;
            bombsRemaining = bombAmount;

            // Blast radius
            int powerUps = PlayerPrefs.GetInt($"Player{playerId}_{Scenes.Shop.ShopItemType.PowerUp}", 0);
            explosionRadius += powerUps;

            // Timebomb toggle
            if (PlayerPrefs.GetInt($"Player{playerId}_{Scenes.Shop.ShopItemType.Timebomb}", 0) == 1)
                timeBomb = true;

            // Remote bomb toggle
            if (PlayerPrefs.GetInt($"Player{playerId}_{Scenes.Shop.ShopItemType.Controller}", 0) == 1)
                remoteBomb = true;

            Debug.Log($"[BombController] Player {playerId} upgrades applied: bombs={bombAmount}, radius={explosionRadius}, timeBomb={timeBomb}, remoteBomb={remoteBomb}");
        }
    }
}
