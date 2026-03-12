using Core;
using Scenes.Arena;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player;
using Scenes.Arena.Player.Abilities;
using Unity.Netcode;
using UnityEngine;

namespace Scenes.Arena.Map
{
    public class ItemPickup : NetworkBehaviour
    {
        public enum ItemType
        {
            ExtraBomb,
            BlastRadius,
            Superman,
            Protection,
            Ghost,
            SpeedIncrease,
            Death,
            Random,
            TimeBomb,
            Stop,
            Coin,
            RemoteBomb,
        }

        public ItemType type;

        private GameManager _gameManager;

        private void OnEnable()
        {
            var root = transform.root != transform ? transform.root : null;
            _gameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                           ?? GameManager.Instance;
            _gameManager?.RegisterItem(this);
        }

        private void OnDestroy()
        {
            _gameManager?.UnregisterItem(this);
        }

        // ItemPickup.cs
        public static void ApplyItem(GameObject player, ItemType type)
        {
            var bombCtrl = player.GetComponent<BombController>();
            var playerCtrl = player.GetComponent<PlayerController>();
            var ghostCtrl = player.GetComponentInChildren<Ghost>();
            var protectionCtrl = player.GetComponentInChildren<Protection>();
            var supermanCtrl = player.GetComponentInChildren<Superman>();

            switch (type)
            {
                case ItemType.ExtraBomb:
                    bombCtrl.AddBomb();
                    AudioController.I.PlayPowerUp();
                    break;

                case ItemType.BlastRadius:
                    bombCtrl.IncreaseBlastRadius();
                    AudioController.I.PlayBombPickup();
                    break;

                case ItemType.Superman:
                    supermanCtrl.Activate();
                    AudioController.I.PlaySuperman();
                    break;

                case ItemType.Protection:
                    protectionCtrl.Activate();
                    AudioController.I.PlayProtection();
                    break;

                case ItemType.Ghost:
                    ghostCtrl.Activate();
                    AudioController.I.PlayGhost();
                    break;

                case ItemType.SpeedIncrease:
                    playerCtrl.IncreaseSpeed();
                    AudioController.I.PlaySpeedUp();
                    break;

                case ItemType.Coin:
                    playerCtrl.AddCoin();
                    AudioController.I.PlayCoin();
                    break;

                case ItemType.TimeBomb:
                    bombCtrl.EnableTimeBomb();
                    AudioController.I.PlayBombPickup();
                    break;

                case ItemType.Stop:
                    playerCtrl.ActivateStop();
                    AudioController.I.PlayPowerUp();
                    break;

                case ItemType.RemoteBomb:
                    bombCtrl.EnableRemoteBomb();
                    AudioController.I.PlayBombPickup();
                    break;

                case ItemType.Death:
                    playerCtrl.ApplyDeath();
                    break;

                case ItemType.Random:
                    playerCtrl.ApplyRandom(); // recursion safe
                    break;
            }
        }

        private void OnItemPickup(GameObject player)
        {
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline)
            {
                // Online: only host resolves pickup; broadcasts to all clients.
                if (!IsServer) return;
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    ApplyItemClientRpc((int)type, pc.playerId);
                Destroy(gameObject);
            }
            else
            {
                ApplyItem(player, type);
                Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void ApplyItemClientRpc(int itemType, int playerId)
        {
            // Find the player with this ID and apply the item.
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in players)
            {
                if (pc.playerId == playerId)
                {
                    ApplyItem(pc.gameObject, (ItemType)itemType);
                    return;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnItemPickup(other.gameObject);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            {
                // Explosion destroys the item
                Destroy(gameObject);
            }
        }
    }
}
