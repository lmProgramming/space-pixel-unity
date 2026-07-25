using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Gameplay.Progression;
using Core.Services;
using Core.State;
using Services;
using UI.MVCVM;
using UI.Scenes.BattleShipPicker.Views;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace UI.Scenes.NextBattle.Views
{
    [RequireComponent(typeof(NextBattlePickerView))]
    public class
        NextBattlePickerController : Controller<NextBattlePickerModel, NextBattlePickerView, NextBattlePickerViewModel>
    {
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
            var save = _progressionRepository.Load(SaveState.ProgressionSlotIndex);
            return new NextBattlePickerViewModel(CreateEntries(save));
        }

        private static IReadOnlyList<BattleShipPickerEntry> CreateEntries(ProgressionSave save)
        {
            if (save.allies == null || save.allies.Length == 0)
                throw new InvalidOperationException("[BattleShipPicker] Progression save has no allies.");

            var entries = new BattleShipPickerEntry[save.allies.Length];

            for (var index = 0; index < save.allies.Length; index++)
            {
                var snapshot = save.allies[index];
                entries[index] = new BattleShipPickerEntry(
                    index,
                    string.IsNullOrWhiteSpace(snapshot.shipName) ? $"Ship {index + 1}" : snapshot.shipName,
                    ShipPreviewIconCompositor.ComposeFromSnapshot(snapshot));
            }

            return entries;
        }

        private static void OnConfirmClicked(int allyIndex)
        {
            SaveState.SelectedAllyIndex = allyIndex;
            SceneManager.LoadScene(SceneNames.MainGame);
        }
    }
}