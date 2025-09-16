using Core;
using Scenes.Arena.Bomb;
using Scenes.Arena.Player;
using Scenes.Arena.Player.Abilities;
using UnityEngine;

namespace Scenes.Arena.Map
{
    public class ItemPickup : MonoBehaviour
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
            ApplyItem(player, type);
            Destroy(gameObject);
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
