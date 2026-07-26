using System;
using Core.Constants;
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
        [Inject] private INextBattleService _nextBattleService;
        [Inject] private IProgressionRepository _progressionRepository;

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
            var entries = _nextBattleService.GetNextBattlePickerEntries();
            return new NextBattlePickerViewModel(entries);
        }

        private static void OnConfirmClicked(Guid allyIndex)
        {
            SaveState.SelectedBattleId = allyIndex;
            SceneManager.LoadScene(SceneNames.MainGame);
        }
    }
}