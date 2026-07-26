using Core.Progression;

namespace Core.Services
{
    public interface INextBattleService
    {
        NextBattlePickerEntry[] GetNextBattlePickerEntries();
    }
}