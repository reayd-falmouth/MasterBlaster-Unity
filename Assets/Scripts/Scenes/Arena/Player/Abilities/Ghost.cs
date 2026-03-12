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
        private Rigidbody2D rb;
        private LayerMask ghostExcludeLayers;
        private bool active;
        private float timer;
        private Coroutine endCo;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            rb = GetComponentInParent<Rigidbody2D>();
            ghostExcludeLayers = LayerMask.GetMask("Destructible", "Bomb");
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

        private void OnDisable()
        {
            if (!active) return;
            active = false;

            // Stop any running end coroutine so it doesn't double-restore
            if (endCo != null) { StopCoroutine(endCo); endCo = null; }

            // Restore per-rigidbody exclude layers immediately.
            if (rb != null)
                rb.excludeLayers &= ~ghostExcludeLayers;

            // Clear visual overrides
            if (pc != null)
            {
                pc.visualOverrideActive = false;
                pc.visualOverrideRenderer = null;
            }
            if (spriteRendererGhost != null)
                spriteRendererGhost.StopAnimation();
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

            // Ignore collisions for this player's rigidbody only
            rb.excludeLayers |= ghostExcludeLayers;
        }

        private IEnumerator EndRoutine()
        {
            active = false;

            // Restore collisions for this player's rigidbody only
            rb.excludeLayers &= ~ghostExcludeLayers;

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
