using System;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Events.Game;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Services
{
    public class MissionService : MonoBehaviour, IMissionService
    {
        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;
        [Inject] private BattleDefeatEventChannel _battleDefeatEventChannel;

        [Inject] private BattleVictoryEventChannel _battleVictoryEventChannel;
        private bool _missionOver;

        private ITeam _playerTeam;
        [Inject] private IShipService _shipService;

        private void Start()
        {
            if (_activePlayerShipProvider.ActiveShip == null)
                throw new UnityException("[MissionService] Active player ship is not set.");

            _playerTeam = _activePlayerShipProvider.ActiveShip.Team;
        }

        private void Update()
        {
            if (_missionOver)
                return;

            if (AllEnemiesDestroyed())
                TriggerVictory();

            if (AllAlliesDestroyed())
                TriggerDefeat();
        }

        public event Action OnVictory;
        public event Action OnDefeat;

        private bool AllEnemiesDestroyed()
        {
            return !_shipService.GetEnemyShipsOf(_playerTeam).AsValueEnumerable().Any();
        }

        private bool AllAlliesDestroyed()
        {
            return !_shipService.GetAlliedShipsOf(_playerTeam).AsValueEnumerable().Any();
        }

        private void TriggerVictory()
        {
            _missionOver = true;
            Debug.Log("[MissionService] Victory!");
            OnVictory?.Invoke();
            _battleVictoryEventChannel.Raise();
        }

        private void TriggerDefeat()
        {
            _missionOver = true;
            Debug.Log("[MissionService] Defeat!");
            OnDefeat?.Invoke();
            _battleDefeatEventChannel.Raise();
        }
    }
}