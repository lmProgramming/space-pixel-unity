using Core.Gameplay.Progression;
using Core.Progression;

namespace Core.Services
{
    public interface IProgressionRepository
    {
        ProgressionSlotsModel Model { get; }

        bool SlotHasSave(int slotIndex);

        ProgressionSave Load(int slotIndex);

        void Save(int slotIndex, ProgressionSave save);

        void Delete(int slotIndex);
    }
}