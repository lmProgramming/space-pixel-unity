using System;
using Core.Constants;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ships;
using Core.State;
using Events.Game.BattleOver;
using LMPro.External.IsAlive;
using UI.Scenes.MainGame.Views.ProgressionGameOver;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using ZLinq;

namespace UI.Scenes.MainGame.Views
{
    public class ProgressionBattleResolutionHandler : MonoBehaviour, IBattleResolutionHandler
    {
        [SerializeField] private ProgressionGameOverController progressionGameOverController;
        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;
        [Inject] private BattleOverEventChannel _battleOverEventChannel;

        [Inject] private IProgressionRepository _progressionRepository;
        [Inject] private IShipService _shipService;
        [Inject] private IShipSnapshotService _snapshotService;

        private void Start()
        {
            if (SaveState.Mode != GameSessionMode.Progression) Destroy(this);
        }

        private void OnEnable()
        {
            _battleOverEventChannel.Register(HandleBattleOver);
        }

        private void OnDisable()
        {
            _battleOverEventChannel.Unregister(HandleBattleOver);
        }

        public void OnBattleVictory()
        {
            var save = _progressionRepository.Load(SaveState.ProgressionSlotIndex);
            save.allies = CaptureSurvivingAllySnapshots(_activePlayerShipProvider.ActiveShip.Team);
            save.enemiesKilled++;
            _progressionRepository.Save(SaveState.ProgressionSlotIndex, save);

            SceneManager.LoadScene(SceneNames.BattleShipPicker);
        }

        public void OnBattleDefeat()
        {
            var save = _progressionRepository.Load(SaveState.ProgressionSlotIndex);
            progressionGameOverController.Show(save.campaignName, save.credits, save.enemiesKilled);
            _progressionRepository.Delete(SaveState.ProgressionSlotIndex);
        }

        private void HandleBattleOver(BattleOverData battleOverData)
        {
            switch (battleOverData.Result)
            {
                case BattleResult.FriendlyWin:
                    OnBattleVictory();
                    break;
                case BattleResult.EnemyWin:
                    OnBattleDefeat();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ShipSnapshot[] CaptureSurvivingAllySnapshots(ITeam playerTeam)
        {
            return (from ship in _shipService.GetAlliedShipsOf(playerTeam).AsValueEnumerable()
                where ship.IsAlive()
                select _snapshotService.CaptureSnapshot(ship)
                into snapshot
                where snapshot != null
                select snapshot).ToArray();
        }
    }
}