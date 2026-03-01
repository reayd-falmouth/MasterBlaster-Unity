using Core;
using Scenes.Arena.Bomb;
using Scenes.Arena.Map;
using Scenes.Shop;
using UnityEngine;

namespace Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Superman : MonoBehaviour
    {
        private PlayerController pc;
        private BombController bombController;
        private bool active;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            bombController = GetComponentInParent<BombController>();
        }

        private void Start()
        {
            ApplyUpgrades();
        }

        private void FixedUpdate()
        {
            if (!active)
                return;

            // Only push if the player is moving
            if (pc.Direction != Vector2.zero)
            {
                TryConvertDestructibleAhead(pc.Direction);
            }
        }

        public void Activate()
        {
            active = true;
        }

        private void ApplyUpgrades()
        {
            var playerId = pc.playerId;
            active = SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Superman) == 1;
            Debug.Log($"[PlayerController] Player {playerId} superman applied.");
        }

        private void TryConvertDestructibleAhead(Vector2 direction)
        {
            if (bombController == null || bombController.destructibleTiles == null)
                return;

            // look one cell ahead
            Vector2 ahead = (Vector2)transform.position + direction;
            Vector3Int cell = bombController.destructibleTiles.WorldToCell(ahead);
            var tile = bombController.destructibleTiles.GetTile(cell);

            if (tile != null)
            {
                // remove the tile
                bombController.destructibleTiles.SetTile(cell, null);

                // spawn destructible prefab in its place
                Vector3 worldPos =
                    bombController.destructibleTiles.CellToWorld(cell)
                    + new Vector3(0.5f, 0.5f, 0f);
                var newBlock = Instantiate(
                    bombController.destructiblePrefab,
                    worldPos,
                    Quaternion.identity
                );

                // mark this as a pushable block, not debris
                var destructible = newBlock.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.isDebris = false;
                }
            }
        }

        public bool IsActive => active;
    }
}
