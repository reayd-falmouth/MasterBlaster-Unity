using Core;
using MoreMountains.Feedbacks;
using Scenes.Arena;
using UnityEngine;

namespace Scenes.Arena.Bomb
{
    public class Explosion : MonoBehaviour
    {
        public AnimatedSpriteRenderer spriteRenderer;

        [Tooltip("MMF_Player on this prefab — add an MMF_MMSoundManagerSound feedback and assign your explode clip there.")]
        [SerializeField] private MMF_Player explosionFeedbacks;

        private GameManager _gameManager;

        private void Awake()
        {
            var root = transform.root != transform ? transform.root : null;
            _gameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                           ?? GameManager.Instance;
            _gameManager?.RegisterExplosion(this);
        }

        private void OnDestroy()
        {
            _gameManager?.UnregisterExplosion(this);
        }

        /// <summary>Plays the explosion feedbacks (call only on the central explosion).</summary>
        public void PlayExplosionSound()
        {
            explosionFeedbacks?.PlayFeedbacks(transform.position);
        }

        public void SetDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x);
            transform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
        }

        public void DestroyAfter(float seconds)
        {
            float soundDuration = explosionFeedbacks != null ? explosionFeedbacks.TotalDuration : 0f;
            Destroy(gameObject, Mathf.Max(seconds, soundDuration));
        }
    }
}

