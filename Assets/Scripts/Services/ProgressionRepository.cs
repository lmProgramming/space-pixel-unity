using System;
using System.IO;
using Core.Constants;
using Core.Gameplay.Progression;
using Core.Progression;
using Core.Services;
using UnityEngine;

namespace Services
{
    public class ProgressionRepository : IProgressionRepository
    {
        public ProgressionRepository()
        {
            Model = new ProgressionSlotsModel();
            Refresh();
        }

        public ProgressionSlotsModel Model { get; }

        public bool SlotHasSave(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return File.Exists(GetSlotFilePath(slotIndex));
        }

        public ProgressionSave Load(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            var filePath = GetSlotFilePath(slotIndex);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Progression save for slot {slotIndex} was not found.", filePath);

            var json = File.ReadAllText(filePath);
            var save = JsonUtility.FromJson<ProgressionSave>(json);

            return save ??
                   throw new InvalidOperationException($"Failed to deserialize progression save for slot {slotIndex}.");
        }

        public void Save(int slotIndex, ProgressionSave save)
        {
            ValidateSlotIndex(slotIndex);

            if (save == null)
                throw new ArgumentNullException(nameof(save));

            var directoryPath = Constants.ProgressionSavesFolder;
            Directory.CreateDirectory(directoryPath);

            var json = JsonUtility.ToJson(save, true);
            File.WriteAllText(GetSlotFilePath(slotIndex), json);

            Refresh();
        }

        public void Delete(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);

            var filePath = GetSlotFilePath(slotIndex);
            if (File.Exists(filePath))
                File.Delete(filePath);

            Refresh();
        }

        private void Refresh()
        {
            var descriptors = new ProgressionSlotDescriptor[Constants.ProgressionSlotCount];

            for (var slotIndex = 0; slotIndex < Constants.ProgressionSlotCount; slotIndex++)
            {
                if (!SlotHasSave(slotIndex))
                {
                    descriptors[slotIndex] = new ProgressionSlotDescriptor(slotIndex, false, string.Empty);
                    continue;
                }

                var save = Load(slotIndex);
                descriptors[slotIndex] = new ProgressionSlotDescriptor(slotIndex, true, save.campaignName);
            }

            Model.ReplaceAll(descriptors);
        }

        private static string GetSlotFilePath(int slotIndex)
        {
            return Path.Combine(Constants.ProgressionSavesFolder, Constants.ProgressionSlotFileName(slotIndex));
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex is < 0 or >= Constants.ProgressionSlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}