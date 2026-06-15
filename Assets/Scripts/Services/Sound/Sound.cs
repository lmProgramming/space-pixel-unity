using System;
using Core.Gameplay.Sound;
using UnityEngine;

namespace Services.Sound
{
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
        public float maxDistance = -1;
    }
}