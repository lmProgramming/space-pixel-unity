using System;
using Core.Constants;
using Core.Gameplay;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Events.Game.BattleOver;
using Gameplay.EasyTeam;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Services
{
    public class MissionService : MonoBehaviour, IMissionService
    {
        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;

        [Inject] private BattleOverEventChannel _battleOverEventChannel;
        private bool _missionOver;

        private bool _missionStarted;

        private ITeam _missionTeam;
        [Inject] private IShipService _shipService;

        private void Update()
        {
            if (_missionOver || !_missionStarted)
                return;

            var allAlliesDestroyed = AllAlliesDestroyed();
            var allEnemiesDestroyed = AllEnemiesDestroyed();

            if (allEnemiesDestroyed && !allAlliesDestroyed)
                TriggerVictory();

            if (allAlliesDestroyed && !allEnemiesDestroyed)
                TriggerDefeat();
        }

        public event Action OnVictory;
        public event Action OnDefeat;

        public void Setup()
        {
            TryBindMissionTeam();

            _missionStarted = true;
        }

        public void SetMissionStarted(bool started)
        {
            _missionStarted = started;
        }

        private void TryBindMissionTeam()
        {
            if (_missionTeam != null) return;

            if (_activePlayerShipProvider.HasPlayerShip)
            {
                _missionTeam = _activePlayerShipProvider.ActiveShip.Team;
                return;
            }

            var friendlyShip = _shipService.GetShips().AsValueEnumerable()
                .FirstOrDefault(ship =>
                    ship.Team is Team team && team.Layer == PhysicsLayers.Friendly);

            if (friendlyShip == null) return;

            _missionTeam = friendlyShip.Team;
        }

        private bool AllAlliesDestroyed()
        {
            return !_shipService.GetAlliedShipsOf(_missionTeam).AsValueEnumerable().Any();
        }

        private bool AllEnemiesDestroyed()
        {
            return !_shipService.GetEnemyShipsOf(_missionTeam).AsValueEnumerable().Any();
        }

        private void TriggerVictory()
        {
            _missionOver = true;
            Debug.Log("[MissionService] Victory!");
            OnVictory?.Invoke();
            _battleOverEventChannel.Raise(new BattleOverData
            {
                Result = BattleResult.FriendlyWin
            });
        }

        private void TriggerDefeat()
        {
            _missionOver = true;
            Debug.Log("[MissionService] Defeat!");
            OnDefeat?.Invoke();
            _battleOverEventChannel.Raise(new BattleOverData
            {
                Result = BattleResult.EnemyWin
            });
        }
    }
}