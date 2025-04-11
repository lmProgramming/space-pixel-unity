using System;
using System.Collections.Generic;
using UnityEngine;

namespace LM
{
    public enum SoundIdentifier
    {
        Explosion
    }

    public class SoundManager : MonoBehaviour
    {
        public Sound[] sounds;
        private Dictionary<SoundIdentifier, Sound> _soundsDictionary;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _soundsDictionary = new Dictionary<SoundIdentifier, Sound>();

            foreach (var sound in sounds)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;

                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.isLoop;

                _soundsDictionary[sound.identifier] = sound;
            }
        }

        private void Start()
        {
            SetVolume(PlayerPrefs.GetFloat("soundVolume", 1f));
        }

        public void Play(SoundIdentifier identifier)
        {
            var sound = GetSound(identifier);

            if (sound == null)
            {
                Debug.LogError("Sound " + identifier + " Not Found!");
                return;
            }

            if (!CanPlaySound(sound)) return;

            sound.source.Play();
        }

        public void Stop(SoundIdentifier identifier)
        {
            var sound = GetSound(identifier);

            if (sound == null)
            {
                Debug.LogError("Sound " + identifier + " Not Found!");
                return;
            }

            sound.source.Stop();
        }

        public void Pause(SoundIdentifier identifier)
        {
            var sound = GetSound(identifier);

            if (sound == null)
            {
                Debug.LogError("Sound " + identifier + " Not Found!");
                return;
            }

            sound.source.Pause();
        }

        public void UnPause(SoundIdentifier identifier)
        {
            var sound = GetSound(identifier);

            if (sound == null)
            {
                Debug.LogError("Sound " + identifier + " Not Found!");
                return;
            }

            sound.source.UnPause();
        }

        public bool PlayingSound(SoundIdentifier identifier)
        {
            var sound = GetSound(identifier);

            if (sound != null) return sound.source.isPlaying;

            Debug.LogError("Sound " + identifier + " Not Found!");
            return false;
        }

        private Sound GetSound(SoundIdentifier identifier)
        {
            var sound = _soundsDictionary[identifier];
            return sound;
        }

        private bool CanPlaySound(Sound sound)
        {
            return _soundsDictionary.TryGetValue(sound.identifier, out _);
        }

        private void SetVolume(float val)
        {
            foreach (var t in sounds) t.source.volume = val * t.volume;
        }

        public void SetVolumeOfSoundEffects(float val)
        {
            foreach (var t in sounds)
                if (t.type == Sound.Type.Effect)
                    t.source.volume = val * t.volume;
        }

        public void SetVolumeOfMusicTracks(float val)
        {
            foreach (var t in sounds)
                if (t.type == Sound.Type.Music)
                    t.source.volume = val * t.volume;
        }
    }

    [Serializable]
    public class Sound
    {
        public enum Type
        {
            Effect,
            Music
        }

        public SoundIdentifier identifier;

        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;

        [Range(.1f, 3f)] public float pitch = 1f;

        public bool isLoop;
        public AudioSource source;

        public Type type;
    }
}