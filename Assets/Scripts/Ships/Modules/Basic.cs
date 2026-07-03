using System;
using System.Runtime.CompilerServices;
using Core.Services;
using Core.Ship;
using Core.Ship.ModuleSnapshotPayloads;
using UnityEngine;

[assembly: InternalsVisibleTo("Ships.Tests")]

namespace Ships.Modules
{
    public class Basic : Module
    {
        [SerializeField]
        private ModuleType moduleType;

        public override ModuleType Type => moduleType;

        protected override void Start()
        {
            base.Start();

            if (Type is ModuleType.Engine or ModuleType.Command or ModuleType.Weapon)
                throw new UnityException("[Basic] Wrong Type assigned as ModuleType");
        }

        private void OnValidate()
        {
            Type = moduleType;
        }

        public override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new BasicModuleData
            {
                moduleType = moduleType
            };

            return JsonUtility.ToJson(data);
        }

        public override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                throw new ArgumentException("[Basic] typePayloadJson cannot be null or whitespace.");

            var data = JsonUtility.FromJson<BasicModuleData>(typePayloadJson);
            if (data == null)
                throw new ArgumentException("[Basic] typePayloadJson cannot be null or whitespace.");

            moduleType = data.moduleType;
        }

#if UNITY_INCLUDE_TESTS
        internal void InitializeForTesting(ModuleType moduleType)
        {
            this.moduleType = moduleType;
        }
#endif
    }
}