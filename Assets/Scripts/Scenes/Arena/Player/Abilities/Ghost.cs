using System.Collections;
using Core;
using Scenes.Shop;
using UnityEngine;

namespace Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Ghost : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private float defaultDuration = 15f;

        [SerializeField]
        private AnimatedSpriteRenderer spriteRendererGhost;

        private PlayerController pc;
        private bool active;
        private float timer;
        private Coroutine endCo;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
        }

        private void Start()
        {
            ApplyUpgrades();
        }

        private void Update()
        {
            if (!active)
                return;

            timer -= Time.deltaTime;
            if (timer <= 0f && endCo == null)
                endCo = StartCoroutine(EndRoutine());
        }

        void ApplyUpgrades()
        {
            if (SessionManager.Instance == null)
            {
                active = false;
                return;
            }
            var playerId = pc.playerId;
            active = SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Ghost) == 1;
            Debug.Log($"[PlayerController] Player {playerId} ghost applied.");
        }

        public void Activate(float duration = -1f)
        {
            active = true;
            timer = duration > 0f ? duration : defaultDuration;

            // Tell PlayerController to override visuals with the ghost renderer
            spriteRendererGhost.StartAnimation();
            pc.visualOverrideActive = true;
            pc.visualOverrideRenderer = spriteRendererGhost;
            pc.UpdateVisualState(); // force refresh

            // Ignore collisions
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Destructible"),
                true
            );
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Bomb"),
                true
            );
        }

        private IEnumerator EndRoutine()
        {
            active = false;

            // Restore collisions
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Destructible"),
                false
            );
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Player"),
                LayerMask.NameToLayer("Bomb"),
                false
            );

            // Clear override → back to normal visuals
            pc.visualOverrideActive = false;
            pc.visualOverrideRenderer = null;
            pc.SetVisualState(PlayerController.PlayerVisualState.Normal);
            spriteRendererGhost.StopAnimation();

            // Safety check
            if (
                Physics2D.OverlapCircle(
                    transform.position,
                    0.1f,
                    LayerMask.GetMask("Destructible", "Bomb")
                )
            )
                pc.ApplyDeath();

            yield return null;
            endCo = null;
        }
    }
}
