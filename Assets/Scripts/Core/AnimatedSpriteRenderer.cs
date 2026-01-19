using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatedSpriteRenderer : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        public Sprite idleSprite;
        public Sprite[] animationSprites;

        public float animationTime = 0.25f;
        private int animationFrame;

        public bool loop = true;
        public bool idle = true;

        // NEW: only animate when explicitly told
        public bool playOnStart = true;
        private bool animating = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            spriteRenderer.enabled = true;
        }

        private void OnDisable()
        {
            spriteRenderer.enabled = false;
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartAnimation();
            }
            else
            {
                spriteRenderer.sprite = idleSprite;
            }
        }

        public void StartAnimation()
        {
            if (animating) return;
            animating = true;
            InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
        }

        public void StopAnimation()
        {
            animating = false;
            CancelInvoke(nameof(NextFrame));
            spriteRenderer.sprite = idleSprite;
        }

        private void NextFrame()
        {
            if (!animating) return;

            animationFrame++;

            if (loop && animationFrame >= animationSprites.Length) {
                animationFrame = 0;
            }

            if (idle) {
                spriteRenderer.sprite = idleSprite;
            } else if (animationFrame >= 0 && animationFrame < animationSprites.Length) {
                spriteRenderer.sprite = animationSprites[animationFrame];
            }
        }
    }
}