using System;
using Core.Gameplay.Sound;
using UnityEngine;
using Zenject;

namespace ShipFactory.UI.Runtime
{
    public class ShipFactoryFeedback : MonoBehaviour
    {
        [SerializeField] private GameObject burstPrefab;

        [Inject] private readonly ISoundManager _soundManager;

        private void Start()
        {
            if (_soundManager == null) throw new ArgumentNullException(nameof(_soundManager));
        }

        public void PlayPlaced(Vector2 worldPosition)
        {
            // PlayBurst(worldPosition);
            PlaySound(SoundIdentifier.ModulePlace, worldPosition);
        }

        public void PlayDeleted(Vector2 worldPosition)
        {
            // PlayBurst(worldPosition);
            PlaySound(SoundIdentifier.ModuleDelete, worldPosition);
        }

        public void PlayRotated(Vector2 worldPosition)
        {
            PlaySound(SoundIdentifier.ModuleRotate, worldPosition);
        }

        private void PlaySound(SoundIdentifier identifier, Vector2 worldPosition)
        {
            _soundManager.Play(identifier, worldPosition);
        }

        // private void PlayBurst(Vector2 worldPosition)
        // {
        //     Instantiate(burstPrefab, worldPosition, Quaternion.identity);
        // }
    }
}