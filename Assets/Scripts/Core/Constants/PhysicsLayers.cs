using UnityEngine;

namespace Core.Constants
{
    public static class PhysicsLayers
    {
        public const string DefaultName = "Default";
        public const string BulletsName = "Bullets";
        public const string DebrisName = "Debris";
        public const string ObstaclesName = "Obstacles";
        public const string FriendlyName = "Friendly";
        public const string EnemyName = "Enemy";

        public static int Default { get; private set; }
        public static int Bullets { get; private set; }
        public static int Debris { get; private set; }
        public static int Obstacles { get; private set; }
        public static int Friendly { get; private set; }
        public static int Enemy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CacheLayerIndices()
        {
            Default = ResolveLayerIndex(DefaultName);
            Bullets = ResolveLayerIndex(BulletsName);
            Debris = ResolveLayerIndex(DebrisName);
            Obstacles = ResolveLayerIndex(ObstaclesName);
            Friendly = ResolveLayerIndex(FriendlyName);
            Enemy = ResolveLayerIndex(EnemyName);
        }

        private static int ResolveLayerIndex(string layerName)
        {
            var index = LayerMask.NameToLayer(layerName);
            if (index < 0)
                throw new UnityException($"[PhysicsLayers] Layer '{layerName}' is not defined in Tag Manager.");
            return index;
        }
    }
}
