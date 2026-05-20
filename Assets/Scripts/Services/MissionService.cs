using System;
using Core;
using Core.Gameplay.EasyTeam;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Services
{
    public class MissionService : MonoBehaviour, IMissionService
    {
        private bool _missionOver;

        [Inject(Id = Constants.PlayerShipId)]
        private IShip _playerShip;

        private ITeam _playerTeam;

        [Inject]
        private IShipService _shipService;

        private void Start()
        {
            _playerTeam = _playerShip.Team;
        }

        private void Update()
        {
            if (_missionOver) return;
            if (AllEnemiesDestroyed()) TriggerVictory();
        }

        private void OnEnable()
        {
            if (_playerShip?.CommandModule?.PixelatedRigidbody == null) return;
            _playerShip.CommandModule.PixelatedRigidbody.OnNoPixelsLeft += HandlePlayerDestroyed;
        }

        private void OnDisable()
        {
            if (_playerShip?.CommandModule?.PixelatedRigidbody == null) return;
            _playerShip.CommandModule.PixelatedRigidbody.OnNoPixelsLeft -= HandlePlayerDestroyed;
        }

        public event Action OnVictory;
        public event Action OnDefeat;

        private bool AllEnemiesDestroyed()
        {
            return !_shipService.GetEnemyShipsOf(_playerTeam).AsValueEnumerable().Any();
        }

        private void TriggerVictory()
        {
            _missionOver = true;
            Debug.Log("[MissionService] Victory!");
            OnVictory?.Invoke();
        }

        private void HandlePlayerDestroyed(IPixelated _)
        {
            if (_missionOver) return;
            _missionOver = true;
            Debug.Log("[MissionService] Defeat!");
            OnDefeat?.Invoke();
        }
    }
}