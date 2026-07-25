using Core.Services;
using UnityEngine;
using Zenject;

namespace Services
{
    [DefaultExecutionOrder(-100)]
    public class SkirmishSetup : MonoBehaviour
    {
        [field: SerializeField]
        public bool SetupMissionService { get; set; } = true;

        [Inject]
        private IMissionService _missionService;

        [Inject]
        private ISkirmishSpawner _skirmishSpawner;

        private void Start()
        {
            _skirmishSpawner.SpawnFromSaveState();

            if (SetupMissionService)
                _missionService.Setup();
        }
    }
}