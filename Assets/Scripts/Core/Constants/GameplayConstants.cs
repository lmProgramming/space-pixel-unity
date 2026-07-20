using UnityEngine;

namespace Core.Constants
{
    [CreateAssetMenu(fileName = "GameplayConstants", menuName = "Constants/GameplayConstants")]
    public class GameplayConstants : ScriptableObject
    {
        public float pixelDamageMultiplier = 0.2f;

        /// <summary>
        ///     A module is destroyed outright once its remaining pixels drop below this fraction
        ///     of its starting pixel count.
        /// </summary>
        public float moduleDestroyedWhenCurrentPixelRatioOfOriginalIsBelow = 0.3f;

        public float chanceOfSpawningExplosionOnDetachingConnectionPoint = 0.3f;
        public float engineThrustEfficiencyMultiplier = 15000f;

        public float nozzleGoingBackToRestRotationMultiplierSpeed = 0.2f;
        public float cannonProjectileSpeedMultiplier = 2000f;
        public float cannonProjectileLifetimeMultiplier = 10f;
    }
}