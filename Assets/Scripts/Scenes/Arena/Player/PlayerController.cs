using System.Collections;
using Core;
using Scenes.Arena.Bomb;
using Scenes.Arena.Map;
using Scenes.Arena.Player.Abilities;
using Scenes.Shop;
using UnityEngine;

namespace Scenes.Arena.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public event System.Func<bool> OnExplosionHit; 
        public Vector2 Direction => direction;

        public enum PlayerVisualState
        {
            Normal,
            Death,
            Remote
        }
        private PlayerVisualState visualState = PlayerVisualState.Normal;

        private Rigidbody2D rb;
        private Vector2 direction = Vector2.down;
        
        [Header("Player Info")]
        public int playerId;
        public int wins = 0;
        public float speed = 5f;
        public int coins = 0;
        public bool stop;
        
        private RemoteBombController pushingBomb;
        
        [Header("Input")]
        public KeyCode inputUp = KeyCode.W;
        public KeyCode inputDown = KeyCode.S;
        public KeyCode inputLeft = KeyCode.A;
        public KeyCode inputRight = KeyCode.D;

        [Header("Sprites")]
        public AnimatedSpriteRenderer spriteRendererUp;
        public AnimatedSpriteRenderer spriteRendererDown;
        public AnimatedSpriteRenderer spriteRendererLeft;
        public AnimatedSpriteRenderer spriteRendererRight;
        public AnimatedSpriteRenderer spriteRendererDeath;
        public AnimatedSpriteRenderer spriteRendererRemoteBomb;
        private AnimatedSpriteRenderer activeSpriteRenderer;

        [HideInInspector] public bool visualOverrideActive;
        [HideInInspector] public AnimatedSpriteRenderer visualOverrideRenderer;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            activeSpriteRenderer = spriteRendererDown;
        }

        private void Start()
        {
            if (playerId <= 0)
            {
                Debug.LogWarning($"[PlayerController] {gameObject.name} has no playerId assigned, defaulting to 1");
                playerId = 1;
            }

            ApplyUpgrades();
        }
        
        private void Update()
        {
            // When controlling a remote bomb, ignore movement input entirely.
            if (visualState == PlayerVisualState.Remote)
                return;

            if (Input.GetKey(inputUp)) {
                SetDirection(Vector2.up, spriteRendererUp);
            } else if (Input.GetKey(inputDown)) {
                SetDirection(Vector2.down, spriteRendererDown);
            } else if (Input.GetKey(inputLeft)) {
                SetDirection(Vector2.left, spriteRendererLeft);
            } else if (Input.GetKey(inputRight)) {
                SetDirection(Vector2.right, spriteRendererRight);
            } else {
                SetDirection(Vector2.zero, activeSpriteRenderer);
            }
        }
        
        private void FixedUpdate()
        {
            Vector2 position = rb.position;

            if (!stop)
            {
                Vector2 translation = speed * Time.fixedDeltaTime * direction;
                rb.MovePosition(position + translation);
            }
        }
        
        private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
        {
            direction = newDirection;
            activeSpriteRenderer = spriteRenderer;
            UpdateVisualState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            {
                // If any subscriber returns true, we cancel death
                if (OnExplosionHit != null)
                {
                    foreach (System.Func<bool> handler in OnExplosionHit.GetInvocationList())
                    {
                        if (handler()) return; // blocked by ability
                    }
                }

                DeathSequence();
            }
        }

        private void DeathSequence()
        {
            enabled = false;
            GetComponent<BombController>().enabled = false;

            visualState = PlayerVisualState.Death;
            UpdateVisualState();

            AudioController.I?.PlayDeath();
            Invoke(nameof(OnDeathSequenceEnded), 1.25f);
        }
        
        private void OnDeathSequenceEnded()
        {
            gameObject.SetActive(false);
            GameManager.Instance.CheckWinState();
        }
        
        public void ApplyUpgrades()
        {
            // Coins
            coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", 0);

            // Speed boost (stackable)
            int speedBoost = PlayerPrefs.GetInt($"Player{playerId}_{ShopItemType.SpeedUp}", 0);
            if (speedBoost > 0) {
                speed += 2 * speedBoost;
                Debug.Log($"[PlayerController] Player {playerId} speed upgraded by {2 * speedBoost}, total speed: {speed}");
            }
            
            Debug.Log($"[PlayerController] Player {playerId} upgrades applied.");
        }
        
        public void ActivateStop(float duration = 10f)
        {
            StartCoroutine(StopRoutine(duration));
        }

        private IEnumerator StopRoutine(float duration)
        {
            stop = true;
            yield return new WaitForSeconds(duration);
            stop = false;
        }

        public void ApplyRandom()
        {
            ItemPickup.ItemType randomType;
            do
            {
                randomType = (ItemPickup.ItemType)Random.Range(
                    0,
                    System.Enum.GetValues(typeof(ItemPickup.ItemType)).Length
                );
            } while (randomType == ItemPickup.ItemType.Random);

            // Give feedback that a random item was rolled
            AudioController.I.PlayPowerUp();

            ItemPickup.ApplyItem(this.gameObject, randomType);
        }

        public void IncreaseSpeed()
        {
            speed++;
            Debug.Log($"[PlayerController] Player {playerId} speed increased to {speed}");
        }

        public void AddCoin()
        {
            coins++;
            PlayerPrefs.SetInt($"Player{playerId}_Coins", coins);
            Debug.Log($"[PlayerController] Player {playerId} coins increased to {coins}");
        }

        public void ApplyDeath()
        {
            DeathSequence();
        }
        
        public void UpdateVisualState()
        {
            // Disable all first
            spriteRendererUp.enabled = false;
            spriteRendererDown.enabled = false;
            spriteRendererLeft.enabled = false;
            spriteRendererRight.enabled = false;
            spriteRendererDeath.enabled = false;
            spriteRendererRemoteBomb.enabled = false;

            // 🔹 If an override is active, only render that
            if (visualOverrideActive && visualOverrideRenderer != null)
            {
                visualOverrideRenderer.enabled = true;
                return;
            }

            // Otherwise use normal visual state
            switch (visualState)
            {
                case PlayerVisualState.Normal:
                    activeSpriteRenderer.enabled = true;
                    activeSpriteRenderer.idle = direction == Vector2.zero;
                    break;
                
                case PlayerVisualState.Death:
                    spriteRendererDeath.enabled = true;
                    break;

                case PlayerVisualState.Remote:
                    spriteRendererRemoteBomb.enabled = true;
                    break;
            }
        }

        public void SetRemoteBombVisual(bool active)
        {
            if (active)
            {
                // enter remote mode: force idle + show remote pose
                direction = Vector2.zero;
                visualState = PlayerVisualState.Remote;
                UpdateVisualState();
            }
            else
            {
                // exit remote mode: back to normal, re-evaluate current renderer
                visualState = PlayerVisualState.Normal;
                UpdateVisualState();
            }
        }
        
        // Inside PlayerController.cs
        public void SetVisualState(PlayerVisualState state)
        {
            visualState = state;
            UpdateVisualState();
        }
        
        private void OnEnable()
        {
            ApplyUpgrades();
        }
    }
}
