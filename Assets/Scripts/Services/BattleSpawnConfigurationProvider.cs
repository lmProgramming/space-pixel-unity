using System;
using Core.Gameplay;
using Core.Services;
using Core.State;

namespace Services
{
    public class BattleSpawnConfigurationProvider : IBattleSpawnConfigurationProvider
    {
        private readonly FreeModeBattleSpawnConfigurationProvider _freeModeProvider;
        private readonly ProgressionBattleSpawnConfigurationProvider _progressionProvider;

        public BattleSpawnConfigurationProvider(
            FreeModeBattleSpawnConfigurationProvider freeModeProvider,
            ProgressionBattleSpawnConfigurationProvider progressionProvider)
        {
            _freeModeProvider = freeModeProvider;
            _progressionProvider = progressionProvider;
        }

        public IBattleSpawnConfiguration GetConfiguration()
        {
            return SaveState.Mode switch
            {
                GameSessionMode.FreeMode => _freeModeProvider.GetConfiguration(),
                GameSessionMode.Progression => _progressionProvider.GetConfiguration(),
                _ => throw new ArgumentOutOfRangeException(nameof(SaveState.Mode), SaveState.Mode, null)
            };
        }
    }
}