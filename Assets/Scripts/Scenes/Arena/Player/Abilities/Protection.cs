using System.Collections;
using Scenes.Shop;
using UnityEngine;

namespace Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Protection : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField]
        private Material whiteProtectionMaterial;

        [SerializeField]
        private float flickerDuration = 2f;

        [SerializeField]
        private float flickerInterval = 0.2f;

        private PlayerController pc;
        private SpriteRenderer[] allSpriteRenderers;
        private Material[] originalMaterials;
        private bool active;
        private Coroutine flickerCo;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            allSpriteRenderers = pc.GetComponentsInChildren<SpriteRenderer>(true);
            originalMaterials = new Material[allSpriteRenderers.Length];
            for (int i = 0; i < allSpriteRenderers.Length; i++)
                originalMaterials[i] = allSpriteRenderers[i].sharedMaterial;
        }

        private void Start()
        {
            ApplyUpgrades();
        }

        void ApplyUpgrades()
        {
            var playerId = pc.playerId;
            active = PlayerPrefs.GetInt($"Player{playerId}_{ShopItemType.Protection}", 0) == 1;
            Debug.Log($"[PlayerController] Player {playerId} protection applied.");
        }

        public void Activate()
        {
            active = true;
            pc.OnExplosionHit += HandleExplosionHit;
            SetProtectionVisual(true);
        }

        private bool HandleExplosionHit()
        {
            if (active)
            {
                TakeDamage(); // absorb and flicker
                return true; // block death
            }
            return false; // not blocking
        }

        public void TakeDamage()
        {
            if (!active)
                return;
            if (flickerCo != null)
                StopCoroutine(flickerCo);
            flickerCo = StartCoroutine(RemoveProtection());
        }

        private IEnumerator RemoveProtection()
        {
            float elapsed = 0f;
            bool on = true;

            while (elapsed < flickerDuration)
            {
                SetProtectionVisual(on);
                on = !on;
                yield return new WaitForSeconds(flickerInterval);
                elapsed += flickerInterval;
            }

            active = false;
            SetProtectionVisual(false);
            pc.OnExplosionHit -= HandleExplosionHit;
        }

        private void SetProtectionVisual(bool on)
        {
            if (whiteProtectionMaterial == null)
            {
                Debug.LogWarning("[Protection] whiteProtectionMaterial not set.");
                return;
            }

            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                allSpriteRenderers[i].material = on
                    ? whiteProtectionMaterial
                    : originalMaterials[i];
            }
        }

        public bool IsActive => active;
    }
}
