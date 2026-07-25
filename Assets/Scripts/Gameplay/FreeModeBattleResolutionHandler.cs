using System;
using Core.Constants;
using Core.Gameplay;
using Core.Services;
using Core.State;
using Core.UI;
using Events.Game.BattleOver;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class FreeModeBattleResolutionHandler : MonoBehaviour, IBattleResolutionHandler
    {
        [Inject] private BattleOverEventChannel _battleOverEventChannel;
        [Inject] private IGameUi _gameUi;

        private void Start()
        {
            if (SaveState.Mode != GameSessionMode.FreeMode) Destroy(this);
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
            if (_gameUi == null)
                throw new InvalidOperationException(
                    "[FreeModeBattleResolutionHandler] IGameUi is not injected.");

            var missionResultUi =
                _gameUi.PushById<IMissionResultUIController>(UIPanelPrefabConstants.MissionResolution);
            missionResultUi.ShowVictory();
        }

        public void OnBattleDefeat()
        {
            if (_gameUi == null)
                throw new InvalidOperationException(
                    "[FreeModeBattleResolutionHandler] IGameUi is not injected.");

            var missionResultUi =
                _gameUi.PushById<IMissionResultUIController>(UIPanelPrefabConstants.MissionResolution);
            missionResultUi.ShowDefeat();
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
    }
}