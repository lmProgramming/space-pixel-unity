using UnityEngine;

namespace LMPro
{
    public static class AudioSourceExt
    {
        public static float Duration(this AudioSource audioSource)
        {
            return audioSource.clip.length / audioSource.pitch;
        }
    }
}