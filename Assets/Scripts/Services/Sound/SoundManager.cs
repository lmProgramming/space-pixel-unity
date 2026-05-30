using System;
using System.Collections.Generic;
using Core;
using Core.Gameplay.Sound;
using EasyPool;
using Instantiation;
using LMPro;
using UnityEngine;

namespace Services.Sound
{
    public class SoundManager : MonoBehaviour, ISoundManager
    {
        public Sound[] sounds = new Sound[1];

        [Header("Pooling Settings")]
        [SerializeField] private GameObject audioSourcePrefab;

        [SerializeField] private Transform audioSourceParent;

        [SerializeField] private Instantiator unityInstantiator;

        [SerializeField] private int maxPoolSize = 50;

        private EasyPool<AudioSource> _audioSourcePool;
        private float _effectsVolume = 1f;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;

        private Dictionary<SoundIdentifier, Sound> _soundsDictionary;
        private readonly Dictionary<SoundIdentifier, List<AudioSource>> _activePooledSources = new();

        private void Awake()
        {
            // Consider singleton pattern alternatives or DI if needed across scenes
            // DontDestroyOnLoad(gameObject);

            if (audioSourcePrefab == null)
            {
                Debug.LogError("SoundManager: AudioSource Prefab is not assigned!", this);
                enabled = false;
                return;
            }

            if (unityInstantiator == null)
            {
                Debug.LogError("SoundManager: Unity Instantiator is not assigned!", this);
                enabled = false;
                return;
            }

            _soundsDictionary = new Dictionary<SoundIdentifier, Sound>();

            try
            {
                _audioSourcePool = new EasyPool<AudioSource>(
                    audioSourcePrefab,
                    audioSourceParent,
                    unityInstantiator,
                    EasyPool<AudioSource>.PoolType.Stack,
                    true,
                    maxPoolSize
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"SoundManager: Failed to initialize AudioSource pool: {e.Message}", this);
                enabled = false;
                return;
            }

            foreach (var sound in sounds)
            {
                if (sound.clips == null)
                {
                    Debug.LogWarning($"Sound '{sound.identifier}' has no AudioClip assigned. Skipping.");
                    continue;
                }

                sound.originalVolume = sound.volume;

                if (!sound.allowOverlap)
                {
                    sound.dedicatedSource = gameObject.AddComponent<AudioSource>();
                    ConfigureAudioSource(sound.dedicatedSource, sound, null);
                    sound.dedicatedSource.playOnAwake = false;
                }

                _soundsDictionary[sound.identifier] = sound;
            }

            Debug.Log(
                $"SoundManager initialized with {_soundsDictionary.Count} sounds and pool for '{audioSourcePrefab.name}'.");
        }


        private void Start()
        {
            _masterVolume = PlayerPrefs.GetFloat(PlayerPrefsKeys.MasterVolume, 1f);
            _musicVolume = PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolume, 1f);
            _effectsVolume = PlayerPrefs.GetFloat(PlayerPrefsKeys.EffectsVolume, 1f);

            ApplyAllVolumes();
        }


        #region Playback Methods

        public void Play(SoundIdentifier identifier, Vector3? position = null)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound))
            {
                Debug.LogError($"Sound '{identifier}' not found in dictionary!");
                return;
            }

            if (!sound.allowOverlap)
                PlayDedicatedSource(sound, position);
            else
                PlayPooledSource(sound, position);
        }

        private void PlayDedicatedSource(Sound sound, Vector3? position)
        {
            if (!sound.dedicatedSource)
            {
                Debug.LogError($"Sound '{sound.identifier}' is marked as non-overlapping but has no dedicated source!");
                return;
            }

            if (sound.dedicatedSource.isPlaying && sound.isLoop) return;

            ConfigureAudioSource(sound.dedicatedSource, sound, position);
            sound.dedicatedSource.Play();
        }

        private void PlayPooledSource(Sound sound, Vector3? position)
        {
            AudioSource audioSource;
            try
            {
                audioSource = _audioSourcePool.Get();
                if (!audioSource || !audioSource)
                {
                    Debug.LogError($"Pool returned invalid object for sound '{sound.identifier}'.", audioSource);
                    if (audioSource) _audioSourcePool.Release(audioSource);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get AudioSource from pool for '{sound.identifier}': {e.Message}", this);
                return;
            }

            ConfigureAudioSource(audioSource, sound, position);
            RegisterActivePooledSource(sound.identifier, audioSource);

            if (audioSource.GetComponent<ReturnToPoolAudioSource>() is { } returner)
            {
                returner.SetOnReturnedToPool(() => UnregisterActivePooledSource(sound.identifier, audioSource));
                returner.OnConfigured();
            }

            audioSource.Play();
        }

        private void ConfigureAudioSource(AudioSource source, Sound sound, Vector3? position)
        {
            if (!source || sound == null || sound.clips.Length == 0) return;

            source.clip = MathExt.RandomFrom(sound.clips);
            source.volume = CalculateActualVolume(sound);
            source.pitch = sound.pitch;
            source.loop = sound.isLoop;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1.0f;
            }
            else
            {
                source.transform.position = transform.position;
                source.spatialBlend = 0f;
            }
        }

        // Stop, Pause, UnPause primarily work reliably for non-overlapping sounds.

        public void Stop(SoundIdentifier identifier)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound))
            {
                Debug.LogError($"Sound '{identifier}' not found in dictionary!");
                return;
            }

            if (!sound.allowOverlap)
            {
                if (sound.dedicatedSource != null)
                    sound.dedicatedSource.Stop();
                return;
            }

            StopAllActivePooledSources(identifier);
        }

        public void Pause(SoundIdentifier identifier)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound))
            {
                Debug.LogError($"Sound '{identifier}' not found in dictionary!");
                return;
            }

            if (!sound.allowOverlap)
            {
                if (sound.dedicatedSource != null)
                    sound.dedicatedSource.Pause();
                return;
            }

            if (!_activePooledSources.TryGetValue(identifier, out var sources))
                return;

            foreach (var source in sources)
                if (source) source.Pause();
        }

        public void UnPause(SoundIdentifier identifier)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound))
            {
                Debug.LogError($"Sound '{identifier}' not found in dictionary!");
                return;
            }

            if (!sound.allowOverlap)
            {
                if (sound.dedicatedSource != null)
                    sound.dedicatedSource.UnPause();
                return;
            }

            if (!_activePooledSources.TryGetValue(identifier, out var sources))
                return;

            foreach (var source in sources)
                if (source) source.UnPause();
        }

        public bool IsPlaying(SoundIdentifier identifier)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound)) return false;

            if (!sound.allowOverlap)
                return sound.dedicatedSource != null && sound.dedicatedSource.isPlaying;

            if (!_activePooledSources.TryGetValue(identifier, out var sources))
                return false;

            foreach (var source in sources)
                if (source && source.isPlaying)
                    return true;

            return false;
        }

        private void RegisterActivePooledSource(SoundIdentifier identifier, AudioSource source)
        {
            if (!_activePooledSources.TryGetValue(identifier, out var sources))
            {
                sources = new List<AudioSource>();
                _activePooledSources[identifier] = sources;
            }

            if (!sources.Contains(source))
                sources.Add(source);
        }

        private void UnregisterActivePooledSource(SoundIdentifier identifier, AudioSource source)
        {
            if (!_activePooledSources.TryGetValue(identifier, out var sources))
                return;

            sources.Remove(source);
            if (sources.Count == 0)
                _activePooledSources.Remove(identifier);
        }

        private void StopAllActivePooledSources(SoundIdentifier identifier)
        {
            if (!_activePooledSources.TryGetValue(identifier, out var sources) || sources.Count == 0)
                return;

            var snapshot = sources.ToArray();
            foreach (var source in snapshot)
            {
                if (!source)
                    continue;

                source.Stop();
                _audioSourcePool.Release(source);
            }

            _activePooledSources.Remove(identifier);
        }

        #endregion

        #region Volume Control

        private float CalculateActualVolume(Sound sound)
        {
            var typeVolume = sound.type == Sound.Type.Music ? _musicVolume : _effectsVolume;
            return Mathf.Clamp01(_masterVolume * typeVolume * sound.originalVolume);
        }

        private void ApplyAllVolumes()
        {
            foreach (var sound in _soundsDictionary.Values)
                if (!sound.allowOverlap && sound.dedicatedSource != null)
                    sound.dedicatedSource.volume = CalculateActualVolume(sound);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.MasterVolume, _masterVolume);
            ApplyAllVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.MusicVolume, _musicVolume);
            ApplyAllVolumes();
        }

        public void SetEffectsVolume(float volume)
        {
            _effectsVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PlayerPrefsKeys.EffectsVolume, _effectsVolume);
            ApplyAllVolumes();
        }

        #endregion
    }

    [Serializable]
    public class Sound
    {
        public enum Type
        {
            Effect,
            Music
        }

        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(.1f, 3f)] public float pitch = 1f;

        [Tooltip("Should this sound loop indefinitely?")]
        public bool isLoop;

        [Tooltip("Allow multiple instances of this sound to play at the same time?")]
        public bool allowOverlap = true;

        public Type type;

        [HideInInspector] public AudioSource dedicatedSource;
        [HideInInspector] public float originalVolume;

        public SoundIdentifier identifier;
    }
}