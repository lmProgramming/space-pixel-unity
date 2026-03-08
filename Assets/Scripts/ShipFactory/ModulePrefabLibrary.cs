using System;
using System.Collections.Generic;
using Core.Ship;
using UnityEngine;
using ZLinq;

namespace ShipFactory
{
    [CreateAssetMenu(fileName = "ModulePrefabLibrary", menuName = "Ship Factory/Module Prefab Library")]
    public class ModulePrefabLibrary : ScriptableObject
    {
        [SerializeField] private List<ModuleTypeEntry> entries = new();

        public IReadOnlyList<ModuleTypeEntry> AllEntries => entries;

        private void OnEnable()
        {
            LogLibraryContents();
        }

        public IReadOnlyList<ShipModuleSO> GetModuleSOsOfType(ModuleType type)
        {
            foreach (var entry in entries.AsValueEnumerable().Where(entry => entry.moduleType == type))
                return entry.prefabs;

            Debug.LogWarning(
                $"[ModulePrefabLibrary] '{name}' — no entry for type {type}. Existing entries: [{string.Join(", ", entries.ConvertAll(e => e.moduleType.ToString()))}]");
            return Array.Empty<ShipModuleSO>();
        }

        private void LogLibraryContents()
        {
            if (entries.Count == 0)
            {
                Debug.LogWarning(
                    $"[ModulePrefabLibrary] '{name}' has NO entries. Open the asset and add ModuleTypeEntry rows, then drag prefabs into each row.",
                    this);
                return;
            }

            foreach (var entry in entries)
            {
                var nullCount = entry.prefabs.FindAll(p => p == null).Count;
                if (nullCount > 0)
                    Debug.LogWarning(
                        $"[ModulePrefabLibrary] '{name}' — type {entry.moduleType} has {nullCount} null prefab slot(s). Check the asset in the Inspector.",
                        this);
                else
                    Debug.Log(
                        $"[ModulePrefabLibrary] '{name}' — type {entry.moduleType}: {entry.prefabs.Count} prefab(s) ready.",
                        this);
            }
        }

        [Serializable]
        public class ModuleTypeEntry
        {
            public ModuleType moduleType;
            public List<ShipModuleSO> prefabs = new();
        }
    }
}