using System;
using System.Runtime.CompilerServices;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.ModuleData;
using UnityEngine;

[assembly: InternalsVisibleTo("Ships.Tests")]

namespace Ships.Modules
{
    public class Basic : Module
    {
        [SerializeField]
        private ModuleType moduleType;

        public override ModuleType Type => moduleType;
        public override ConcreteModuleType ConcreteType => ConcreteModuleType.Basic;

        protected override void Start()
        {
            base.Start();

            if (Type is not ModuleType.Resources and not ModuleType.Structural)
                throw new UnityException("[Basic] Wrong Type assigned as ModuleType");
        }

        protected override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new BasicModuleData
            {
                moduleType = moduleType
            };

            return JsonUtility.ToJson(data);
        }

        protected override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                throw new ArgumentException("[Basic] typePayloadJson cannot be null or whitespace.");

            var data = JsonUtility.FromJson<BasicModuleData>(typePayloadJson);
            if (data == null)
                throw new ArgumentException("[Basic] typePayloadJson was parsed as null.");

            moduleType = data.moduleType;
        }

#if UNITY_INCLUDE_TESTS
        internal void InitializeForTesting(ModuleType newModuleType)
        {
            moduleType = newModuleType;
        }
#endif
    }
}