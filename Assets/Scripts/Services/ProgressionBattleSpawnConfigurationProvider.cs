using System;
using System.Collections.Generic;
using Core.Gameplay;
using Core.Services;
using Core.Ships;
using Core.State;
using UnityEngine;

namespace Services
{
    public class ProgressionBattleSpawnConfigurationProvider : IBattleSpawnConfigurationProvider
    {
        private readonly IProgressionRepository _progressionRepository;

        public ProgressionBattleSpawnConfigurationProvider(IProgressionRepository progressionRepository)
        {
            _progressionRepository = progressionRepository;
        }

        public IBattleSpawnConfiguration GetConfiguration()
        {
            if (SaveState.Mode != GameSessionMode.Progression)
                throw new InvalidOperationException(
                    "[ProgressionBattleSpawnConfigurationProvider] Save state is not in Progression mode.");

            var save = _progressionRepository.Load(SaveState.ProgressionSlotIndex);
            if (save.allies == null || save.allies.Length == 0)
                throw new UnityException(
                    "[ProgressionBattleSpawnConfigurationProvider] Progression save has no allies.");

            var selectedIndex = SaveState.SelectedAllyIndex;
            if (selectedIndex < 0 || selectedIndex >= save.allies.Length)
                throw new ArgumentOutOfRangeException(nameof(SaveState.SelectedAllyIndex));

            var allies = new List<ShipSnapshot>(save.allies.Length - 1);
            for (var index = 0; index < save.allies.Length; index++)
            {
                if (index == selectedIndex)
                    continue;

                allies.Add(save.allies[index]);
            }

            return new BattleSpawnConfiguration(
                save.allies[selectedIndex],
                allies,
                1,
                0,
                0);
        }
    }
}