using System;
using Core.Services;
using Core.State;
using Events.Game.BattleOver;
using UI.Scenes.MainGame.Views.MissionResult;
using UnityEngine;
using Zenject;

namespace UI.Scenes.MainGame.Views
{
    public class FreeModeBattleResolutionHandler : MonoBehaviour, IBattleResolutionHandler
    {
        [SerializeField] private MissionResultUIController missionResultUi;
        [Inject] private BattleOverEventChannel _battleOverEventChannel;

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
            missionResultUi.ShowVictory();
        }

        public void OnBattleDefeat()
        {
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