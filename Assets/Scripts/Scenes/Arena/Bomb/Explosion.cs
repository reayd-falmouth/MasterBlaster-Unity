using Core;
using UnityEngine;

namespace Scenes.Arena.Bomb
{
    public class Explosion : MonoBehaviour
    {
        public AnimatedSpriteRenderer spriteRenderer;

        private AudioSource audioSource;
        private float explosionClipLength;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            if (AudioController.I != null && AudioController.I.SoundFxMixerGroup != null)
                audioSource.outputAudioMixerGroup = AudioController.I.SoundFxMixerGroup;
        }

        /// <summary>Play the explosion sound once on this object's AudioSource (call only on the central explosion). DestroyAfter will delay destruction until the clip finishes.</summary>
        public void PlayExplosionSound()
        {
            if (audioSource == null || AudioController.I == null || AudioController.I.ExplosionClip == null)
                return;
            audioSource.PlayOneShot(AudioController.I.ExplosionClip, 0.8f);
            explosionClipLength = AudioController.I.ExplosionClip.length;
        }

        public void SetDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x);
            transform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
        }

        public void DestroyAfter(float seconds)
        {
            float delay = explosionClipLength > 0 ? Mathf.Max(seconds, explosionClipLength) : seconds;
            Destroy(gameObject, delay);
        }
    }
}
