using Core.Services;
using UnityEngine;
using Zenject;

namespace Services
{
    public class SkirmishSetup : MonoBehaviour
    {
        [Inject]
        private ISkirmishSpawner _skirmishSpawner;

        private void Start()
        {
            _skirmishSpawner.SpawnFromSaveState();
        }
    }
}