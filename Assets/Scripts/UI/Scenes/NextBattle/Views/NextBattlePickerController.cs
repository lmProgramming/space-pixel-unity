using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Progression;
using Core.Services;
using Core.State;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace UI.Scenes.NextBattle.Views
{
    [RequireComponent(typeof(NextBattlePickerView))]
    public class
        NextBattlePickerController : Controller<NextBattlePickerModel, NextBattlePickerView, NextBattlePickerViewModel>
    {
        private IReadOnlyList<NextBattlePickerEntry> _entries;
        [Inject] private INextBattleService _nextBattleService;

        private void Start()
        {
            GameUi.SetRoot(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            View.ConfirmClicked += OnConfirmClicked;
        }

        protected override void OnDisable()
        {
            View.ConfirmClicked -= OnConfirmClicked;
            base.OnDisable();
        }

        protected override NextBattlePickerModel CreateModel()
        {
            return new NextBattlePickerModel();
        }

        protected override NextBattlePickerViewModel CreateViewModel(NextBattlePickerModel model)
        {
            _entries = _nextBattleService.GetNextBattlePickerEntries();
            return new NextBattlePickerViewModel(_entries);
        }

        private void OnConfirmClicked(Guid battleId)
        {
            for (var index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].Id != battleId)
                    continue;

                SaveState.EnemySnapshots = _entries[index].EnemySnapshots;
                SaveState.AsteroidCount = _entries[index].AsteroidsCount;
                SceneManager.LoadScene(SceneNames.BattleShipPicker);
                return;
            }

            throw new InvalidOperationException(
                $"[NextBattlePickerController] No battle entry found for id '{battleId}'.");
        }
    }
}