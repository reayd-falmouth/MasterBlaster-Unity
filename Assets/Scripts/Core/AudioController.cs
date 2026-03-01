using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Utilities;

namespace Core
{
    /// <summary>
    /// Centralised audio manager for HabIt.
    /// - Music: long-form background tracks (Main, Move It, Lullaby, etc.)
    /// - Ambience: relaxation/sleep backgrounds (Pink Noise, Tibetan Bowls)
    /// - SFX: UI clicks, toasts, coin/CredIt transactions
    /// - Voice: ElevenLabs prompts (Awesome!, Move It!, Work It!, Exercise Change)
    /// </summary>
    public class AudioController : PersistentSingleton<AudioController>
    {
        // Quick access singleton alias
        public static AudioController I => Instance;

        [Header("Mixer Routing")]
        [SerializeField]
        public AudioMixer audioMixer;

        [SerializeField]
        public AudioMixerGroup masterMixerGroup; // assign in inspector

        [SerializeField]
        public AudioMixerGroup soundFxMixerGroup; // assign in inspector

        [SerializeField]
        public AudioMixerGroup musicMixerGroup; // assign in inspector

        // Name of the exposed parameter on your mixer
        private const string MasterVolParam = "Master";
        private bool isMuted;

        [Header("Audio Sources (auto-created if missing)")]
        [SerializeField]
        private AudioSource musicSource;

        // [SerializeField] private AudioSource ambienceSource;
        [SerializeField]
        private AudioSource sfxSource;

        [Header("Arena")]
        [SerializeField]
        private AudioClip alarm;

        [SerializeField]
        private AudioClip arenaMusic;

        [Header("Standings")]
        [Tooltip("Oh La La")]
        [SerializeField]
        private AudioClip bingo;

        [Header("Items")]
        [SerializeField]
        private AudioClip powerUp;

        [SerializeField]
        private AudioClip bomb;

        [SerializeField]
        private AudioClip speedUp;

        [SerializeField]
        private AudioClip coin;

        [SerializeField]
        private AudioClip superman;

        [SerializeField]
        private AudioClip protection;

        [SerializeField]
        private AudioClip ghost;

        [Header("Player")]
        [SerializeField]
        private AudioClip die;

        [Header("Bombs")]
        [SerializeField]
        private AudioClip explosion;

        [Header("Objects")]
        [SerializeField]
        private AudioClip moveEffect;

        [Header("Wheel O Fortune")]
        [Tooltip("For the Wheel O Fortune")]
        [SerializeField]
        private AudioClip tick1; // assign in Inspector

        [SerializeField]
        private AudioClip tick2; // assign in Inspector

        [SerializeField]
        private AudioClip chaChing; // assign in Inspector
        private int tickCounter = 0; // Internal tick state for wheel

        [Header("Shop")]
        [SerializeField]
        private AudioClip buy; // assign in Inspector

        [SerializeField]
        private AudioClip noBuy; // assign in Inspector

        [Header("Crossfade Settings")]
        [SerializeField, Range(0.05f, 5f)]
        private float defaultCrossfadeSeconds = 1.0f;

        private int lastTrackIndex = -1; // -1 means "no track has been played yet"

        protected override void Awake()
        {
            base.Awake();
            // menu music should NOT loop so it can end and trigger next track
            EnsureSource(ref musicSource, "Music", loop: false, output: musicMixerGroup);
            // EnsureSource(ref ambienceSource, "Ambience", loop: true, output: musicMixerGroup);
            EnsureSource(ref sfxSource, "SFX", loop: false, output: soundFxMixerGroup);
            EnsureSource(ref sfxSource, "Alarm", loop: false, output: soundFxMixerGroup);
        }

        private void Update()
        {
            // Only run in MainMenu scene
            if (SceneManager.GetActiveScene().name == "Game")
            {
                if (!musicSource.isPlaying)
                {
                    PlayArenaMusic();
                }
            }
            else
            {
                StopMusic();
            }
        }

        void Start()
        {
            // existing
            isMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
            ApplyVolume(); // will set master to -80 dB if muted

            // new
            if (loadSavedVolumesOnStart)
                LoadSavedMixerVolumes(); // pulls "{param}_vol01" and applies to mixer
        }

        #region Public – UI / SFX


        public void PlayExplosion() => PlayOneShotSafe(sfxSource, explosion, 0.8f);

        public void PlayDeath() => PlayOneShotSafe(sfxSource, die, 0.8f);

        #endregion

        #region Public – Item Sounds
        public void PlayPowerUp() => PlayOneShotSafe(sfxSource, powerUp, 1f);

        public void PlayBombPickup() => PlayOneShotSafe(sfxSource, bomb, 1f);

        public void PlaySpeedUp() => PlayOneShotSafe(sfxSource, speedUp, 1f);

        public void PlayCoin() => PlayOneShotSafe(sfxSource, coin, 1f);

        public void PlaySuperman() => PlayOneShotSafe(sfxSource, superman, 1f);

        public void PlayProtection() => PlayOneShotSafe(sfxSource, protection, 1f);

        public void PlayGhost() => PlayOneShotSafe(sfxSource, ghost, 1f);
        #endregion

        #region Public – Music Control

        public void PlayArenaMusic()
        {
            PlayMusic(arenaMusic, true);
        }

        public void StopMusic() => StopSource(musicSource);

        #endregion

        #region Public – Mixer / Volume

        /// <summary>
        /// Set exposed mixer parameter (in dB). Example: SetMixerVolume("MusicVolume", -10f);
        /// </summary>
        public void SetMixerVolume(string exposedParam, float dB)
        {
            if (audioMixer == null)
                return;
            audioMixer.SetFloat(exposedParam, dB);
        }

        /// <summary>
        /// Helper to convert [0..1] linear to decibels for an exposed param.
        /// </summary>
        public void SetMixerVolume01(string exposedParam, float volume01)
        {
            volume01 = Mathf.Clamp01(volume01);
            float dB = volume01 > 0.0001f ? 20f * Mathf.Log10(volume01) : -80f; // mute at ~-80dB
            SetMixerVolume(exposedParam, dB);
        }

        #endregion

        #region Internals

        private void EnsureSource(
            ref AudioSource src,
            string goName,
            bool loop,
            AudioMixerGroup output
        )
        {
            if (src == null)
            {
                var child = new GameObject($"Audio_{goName}");
                child.transform.SetParent(transform);
                src = child.AddComponent<AudioSource>();
            }
            src.playOnAwake = false;
            src.loop = loop;
            src.outputAudioMixerGroup = output;
            src.spatialBlend = 0f; // 2D UI/game mix
        }

        public void PlayMusic(AudioClip clip, bool crossfade)
        {
            if (clip == null)
                return;
            if (!crossfade || !musicSource.isPlaying)
            {
                musicSource.clip = clip;
                musicSource.volume = 1f;
                musicSource.Play();
            }
            else
            {
                StartCoroutine(Crossfade(musicSource, clip, defaultCrossfadeSeconds));
            }
        }

        private void PlayAmbience(AudioClip clip, bool crossfade)
        {
            if (clip == null)
                return;
            if (!crossfade || !musicSource.isPlaying)
            {
                musicSource.clip = clip;
                musicSource.volume = 1f;
                musicSource.Play();
            }
            else
            {
                StartCoroutine(Crossfade(musicSource, clip, defaultCrossfadeSeconds));
            }
        }

        private void StopSource(AudioSource src)
        {
            if (src == null)
                return;
            src.Stop();
            src.clip = null;
        }

        private void PlayOneShotSafe(AudioSource src, AudioClip clip, float volumeScale = 1f)
        {
            if (src == null || clip == null)
                return;
            src.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private IEnumerator Crossfade(AudioSource src, AudioClip toClip, float seconds)
        {
            if (src == null || toClip == null)
                yield break;

            float t = 0f;
            float startVol = src.volume;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime; // UI-safe
                src.volume = Mathf.Lerp(startVol, 0f, t / seconds);
                yield return null;
            }

            src.clip = toClip;
            src.Play();

            t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, 1f, t / seconds);
                yield return null;
            }
            src.volume = 1f;
        }

        #endregion
        [SerializeField]
        private string masterParam = "MasterVol"; // top-level exposed param

        [SerializeField]
        private string musicParam = "MusicVol"; // child group exposed param

        [SerializeField]
        private string sfxParam = "SFXVol"; // child group exposed param

        /// <summary>
        /// Call this from your Button's OnClick()
        /// </summary>
        public void ToggleSound()
        {
            Debug.Log("ToggleSound");
            isMuted = !isMuted;
            PlayerPrefs.SetInt("SoundMuted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            Debug.Log("ApplyVolume");
            // –80 dB is effectively silent; 0 dB is full volume
            float vol = isMuted ? -80f : 0f;
            masterMixerGroup.audioMixer.SetFloat("MasterVol", vol);
        }

        [SerializeField]
        private bool loadSavedVolumesOnStart = true;

        /// <summary>
        /// Load volume settings from PlayerPrefs for the given exposed mixer param
        /// (expects values saved as 0..1 under the key "{param}_vol01") and apply.
        /// </summary>
        private void ApplySaved01(string param)
        {
            if (string.IsNullOrEmpty(param) || audioMixer == null)
                return;

            string key = $"{param}";
            float vol01 = PlayerPrefs.HasKey(key) ? Mathf.Clamp01(PlayerPrefs.GetFloat(key)) : 1f;

            // Push to mixer using existing helper (0..1 -> dB).
            SetMixerVolume01(param, vol01); // uses your existing method.
        }

        /// <summary>
        /// Loads all saved mixer volumes (Master/Music/SFX) from PlayerPrefs.
        /// </summary>
        private void LoadSavedMixerVolumes()
        {
            ApplySaved01(masterParam);
            ApplySaved01(musicParam);
            ApplySaved01(sfxParam);
        }

        public void PlayOhLaLa() => PlayOneShotSafe(sfxSource, bingo, 1.0f);

        /// <summary>
        /// Plays tick sounds in a repeating pattern:
        /// tick1, tick1, tick2, tick1, tick1, tick2, ...
        /// Call this every time the wheel pointer advances.
        /// </summary>
        public void PlayWheelTick()
        {
            tickCounter++;
            if (tickCounter % 3 == 0)
            {
                PlayOneShotSafe(sfxSource, tick2, 0.8f);
            }
            else
            {
                PlayOneShotSafe(sfxSource, tick1, 0.8f);
            }
        }

        /// <summary>
        /// Call this when the wheel stops to play the reward sound.
        /// </summary>
        public void PlayChaChing()
        {
            PlayOneShotSafe(sfxSource, chaChing, 1.0f);
        }

        /// <summary>
        /// Reset tick pattern at start of a spin.
        /// </summary>
        public void ResetWheelTicks()
        {
            tickCounter = 0;
        }

        public void PlayBuy()
        {
            PlayOneShotSafe(sfxSource, buy, 1.0f);
        }

        public void PlayNoBuy()
        {
            PlayOneShotSafe(sfxSource, noBuy, 1.0f);
        }

        // public void PlayObjectMove()
        // {
        //     PlayOneShotSafe(sfxSource, moveEffect, 1.0f);
        // }

        public void PlayObjectMove()
        {
            // 1. Check if the sound is already playing to avoid restarting the loop
            if (sfxSource != null && !sfxSource.isPlaying)
            {
                // 2. Assign the clip and start playing it
                sfxSource.clip = moveEffect;
                sfxSource.loop = true; // IMPORTANT: Set it to loop
                sfxSource.Play();
            }
        }

        public void StopObjectMove()
        {
            // 1. Check if the loop source is playing
            if (sfxSource != null && sfxSource.isPlaying)
            {
                // 2. Stop the playback
                sfxSource.Stop();
            }
        }
    }
}
