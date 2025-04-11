using System;
using System.Collections.Generic;
using UnityEngine;

namespace LM
{
    public class SoundManager : MonoBehaviour
    {
        public Sound[] sounds;
        private static Dictionary<string, float> _soundTimerDictionary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Instance = null;
            _soundTimerDictionary = new Dictionary<string, float>();
        }

        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            DontDestroyOnLoad(gameObject);

            _soundTimerDictionary = new Dictionary<string, float>();

            foreach (var sound in sounds)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;

                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.isLoop;

                _soundTimerDictionary[sound.name] = 0f;
            }
        }

        private void Start()
        {
            SetVolume(PlayerPrefs.GetFloat("soundVolume", 1f));
        }

        public static void Play(string name)
        {
            var sound = Array.Find(Instance.sounds, s => s.name == name);

            if (sound == null)
            {
                Debug.LogError("Sound " + name + " Not Found!");
                return;
            }

            if (!CanPlaySound(sound)) return;

            sound.source.Play();
        }

        public static void Stop(string name)
        {
            var sound = Array.Find(Instance.sounds, s => s.name == name);

            if (sound == null)
            {
                Debug.LogError("Sound " + name + " Not Found!");
                return;
            }

            sound.source.Stop();
        }

        public static void Pause(string name)
        {
            var sound = Array.Find(Instance.sounds, s => s.name == name);

            if (sound == null)
            {
                Debug.LogError("Sound " + name + " Not Found!");
                return;
            }

            sound.source.Pause();
        }

        public static void UnPause(string name)
        {
            var sound = Array.Find(Instance.sounds, s => s.name == name);

            if (sound == null)
            {
                Debug.LogError("Sound " + name + " Not Found!");
                return;
            }

            sound.source.UnPause();
        }

        public static bool PlayingSound(string name)
        {
            var sound = Array.Find(Instance.sounds, s => s.name == name);

            if (sound != null) return sound.source.isPlaying;
            
            Debug.LogError("Sound " + name + " Not Found!");
            return false;
        }

        private static bool CanPlaySound(Sound sound)
        {
            return _soundTimerDictionary.TryGetValue(sound.name, out _);
        }

        public void SetVolume(float val)
        {
            foreach (var t in sounds)
            {
                t.source.volume = val * t.volume;
            }
        }

        public void SetVolumeOfSoundEffects(float val)
        {
            foreach (var t in sounds)
            {
                if (t.type == Sound.Type.Effect)
                {
                    t.source.volume = val * t.volume;
                }
            }
        }

        public void SetVolumeOfMusicTracks(float val)
        {
            foreach (var t in sounds)
            {
                if (t.type == Sound.Type.Music)
                {
                    t.source.volume = val * t.volume;
                }
            }
        }
    }

    [Serializable]
    public class Sound
    {
        public string name;

        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(.1f, 3f)]
        public float pitch = 1f;

        public bool isLoop;
        public bool hasCooldown;
        public AudioSource source;

        public enum Type
        {
            Effect,
            Music
        }

        public Type type;
    }
}