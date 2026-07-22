using Core.Services;
using UnityEngine;
using Zenject;

namespace Services
{
    [DefaultExecutionOrder(-100)]
    public class SkirmishSetup : MonoBehaviour
    {
        [Inject]
        private IMissionService _missionService;

        [Inject]
        private ISkirmishSpawner _skirmishSpawner;

        private void Awake()
        {
            _skirmishSpawner.SpawnFromSaveState();

            _missionService.Setup();
        }
    }
}