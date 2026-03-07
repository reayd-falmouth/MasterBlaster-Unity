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
        private IPlayerInput _inputProvider;

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

        [HideInInspector]
        public bool visualOverrideActive;

        [HideInInspector]
        public AnimatedSpriteRenderer visualOverrideRenderer;

        private AudioSource audioSource;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            activeSpriteRenderer = spriteRendererDown;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            if (AudioController.I != null && AudioController.I.SoundFxMixerGroup != null)
                audioSource.outputAudioMixerGroup = AudioController.I.SoundFxMixerGroup;
        }

        private void Start()
        {
            if (playerId <= 0)
            {
                Debug.LogWarning(
                    $"[PlayerController] {gameObject.name} has no playerId assigned, defaulting to 1"
                );
                playerId = 1;
            }

            _inputProvider = GetComponent<IPlayerInput>();
            ApplyUpgrades();
        }

        private void Update()
        {
            // When controlling a remote bomb, ignore movement input entirely.
            if (visualState == PlayerVisualState.Remote)
                return;

            Vector2 move = _inputProvider != null ? _inputProvider.GetMoveDirection() : GetLegacyMove();
            if (move.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                {
                    if (move.x > 0)
                        SetDirection(Vector2.right, spriteRendererRight);
                    else
                        SetDirection(Vector2.left, spriteRendererLeft);
                }
                else
                {
                    if (move.y > 0)
                        SetDirection(Vector2.up, spriteRendererUp);
                    else
                        SetDirection(Vector2.down, spriteRendererDown);
                }
            }
            else
            {
                SetDirection(Vector2.zero, activeSpriteRenderer);
            }
        }

        private Vector2 GetLegacyMove()
        {
            if (Input.GetKey(inputUp)) return Vector2.up;
            if (Input.GetKey(inputDown)) return Vector2.down;
            if (Input.GetKey(inputLeft)) return Vector2.left;
            if (Input.GetKey(inputRight)) return Vector2.right;
            return Vector2.zero;
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
                        if (handler())
                            return; // blocked by ability
                    }
                }

                DeathSequence();
            }
        }

        private void DeathSequence()
        {
            enabled = false;
            GetComponent<BombController>().enabled = false;

            var rlAgent = GetComponent<Scenes.Arena.Player.AI.BombermanAgent>();
            if (rlAgent != null)
                rlAgent.NotifyDeath();

            visualState = PlayerVisualState.Death;
            UpdateVisualState();

            if (
                audioSource != null
                && AudioController.I != null
                && AudioController.I.DeathClip != null
            )
                audioSource.PlayOneShot(AudioController.I.DeathClip, 0.8f);
            Invoke(nameof(OnDeathSequenceEnded), 1.25f);
        }

        private void OnDeathSequenceEnded()
        {
            gameObject.SetActive(false);
            GameManager.Instance.CheckWinState();
        }

        // public void ApplyUpgrades()
        // {
        //     // Coins
        //     coins = PlayerPrefs.GetInt($"Player{playerId}_Coins", 0);
        //
        //     // Speed boost (stackable)
        //     int speedBoost = PlayerPrefs.GetInt($"Player{playerId}_{ShopItemType.SpeedUp}", 0);
        //     if (speedBoost > 0) {
        //         speed += 2 * speedBoost;
        //         Debug.Log($"[PlayerController] Player {playerId} speed upgraded by {2 * speedBoost}, total speed: {speed}");
        //     }
        //
        //     Debug.Log($"[PlayerController] Player {playerId} upgrades applied.");
        // }

        // Inside PlayerController.cs

        public void ApplyUpgrades()
        {
            // No valid ID yet (e.g. OnEnable fired before GameManager.EnablePlayer assigned one); skip until we have one
            if (playerId <= 0)
                return;
            // SessionManager may not exist in this scene (e.g. Game before Shop); treat as no upgrades
            if (SessionManager.Instance == null)
                return;

            // Reset player stats that are affected by stackable upgrades (like speed, bomb stats)
            // NOTE: You'll need to reset other stats here if they are affected by stackable upgrades.
            // For simplicity, we assume 'speed' starts at 5f (as defined in the header)
            // and this function is called on start/enable.
            speed = 5f;

            // Coins (session-only, from SessionManager)
            coins =
                SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;

            // ---------------------------------------------------------------------------------
            // 🔁 STACKABLE UPGRADES (PowerUp, ExtraBomb, SpeedUp)
            // ---------------------------------------------------------------------------------

            // Speed boost (stackable) - Multiplies base speed
            int speedBoost = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.SpeedUp
            );
            if (speedBoost > 0)
            {
                // Based on existing code: speed is upgraded by 2 units per stack
                speed += 2 * speedBoost;
                Debug.Log(
                    $"[PlayerController] Player {playerId} speed upgraded by {2 * speedBoost}, total speed: {speed}"
                );
            }

            // Extra Bomb (stackable) - Increases max bomb count
            int extraBombCount = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.ExtraBomb
            );
            if (extraBombCount > 0)
            {
                // You will need to access and modify the BombController's bomb limit here.
                // Example: GetComponent<BombController>().AddBombLimit(extraBombCount);
                Debug.Log(
                    $"[PlayerController] Player {playerId} received {extraBombCount} extra bombs."
                );
            }

            // Power Up (stackable) - Increases bomb range/power
            int powerUpCount = SessionManager.Instance.GetUpgradeLevel(
                playerId,
                ShopItemType.PowerUp
            );
            if (powerUpCount > 0)
            {
                // You will need to access and modify the BombController's explosion size/power here.
                // Example: GetComponent<BombController>().AddPower(powerUpCount);
                Debug.Log(
                    $"[PlayerController] Player {playerId} received {powerUpCount} bomb power upgrades."
                );
            }

            // ---------------------------------------------------------------------------------
            // ✅ TOGGLE UPGRADES (Superman, Ghost, Protection, Controller, Timebomb)
            // ---------------------------------------------------------------------------------

            // Superman (Toggle) - Allows walking through walls
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Superman) == 1)
            {
                // You need to add or enable a component/ability script that handles wall-passing.
                // Example: GetComponent<SupermanAbility>().Activate();
                Debug.Log($"[PlayerController] Player {playerId} is Superman.");
            }

            // Ghost (Toggle) - Allows walking through bombs
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Ghost) == 1)
            {
                // You need to add or enable a component/ability script that disables collision with bombs.
                // Example: GetComponent<GhostAbility>().Activate();
                Debug.Log($"[PlayerController] Player {playerId} is a Ghost.");
            }

            // Protection (Toggle) - Protects against one death
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Protection) == 1)
            {
                // You need to add or enable a component/ability script that listens to OnExplosionHit
                // and returns 'true' once to block the death sequence.
                // Example: GetComponent<ProtectionAbility>().Activate();
                Debug.Log($"[PlayerController] Player {playerId} has Protection.");
            }

            // Controller (Toggle) - Allows remote detonation of bombs
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Controller) == 1)
            {
                // You need to enable the remote bomb functionality in the BombController.
                // This is separate from the remote *visual* state.
                // Example: GetComponent<BombController>().EnableRemoteControl();
                Debug.Log($"[PlayerController] Player {playerId} has a Remote Controller.");
            }

            // Timebomb (Toggle) - Allows setting bomb fuse time
            if (SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Timebomb) == 1)
            {
                // You need to enable the ability to set the time in the BombController.
                // Example: GetComponent<BombController>().EnableTimebombFeature();
                Debug.Log($"[PlayerController] Player {playerId} has Timebombs.");
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
                randomType = (ItemPickup.ItemType)
                    Random.Range(0, System.Enum.GetValues(typeof(ItemPickup.ItemType)).Length);
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
            if (SessionManager.Instance != null)
                SessionManager.Instance.AddCoins(playerId, 1);
            coins =
                SessionManager.Instance != null
                    ? SessionManager.Instance.GetCoins(playerId)
                    : coins + 1;
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
