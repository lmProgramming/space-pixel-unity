using UnityEngine;

namespace Core
{
    public interface ISoundManager
    {
        void Play(SoundIdentifier identifier, Vector3? position = null);
        void Stop(SoundIdentifier identifier);
        void Pause(SoundIdentifier identifier);
        void UnPause(SoundIdentifier identifier);
        bool IsPlaying(SoundIdentifier identifier);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetEffectsVolume(float volume);
    }
}