namespace Core.Constants
{
    public static class GameplayConstants
    {
        public const float PixelDamageMultiplier = 0.15f;

        /// <summary>
        ///     A module is destroyed outright once its remaining pixels drop below this fraction
        ///     of its starting pixel count.
        /// </summary>
        public const float ModuleDestroyedBelowPixelRatio = 0.15f;

        public const float ChanceOfSpawningExplosionOnDetachingConnectionPoint = 0.3f;
        public const float EngineThrustEfficiencyMultiplier = 5000f;

        public const float NozzleGoingBackToRestRotationMultiplierSpeed = 0.2f;
        public const float CannonProjectileSpeedMultiplier = 1000f;
        public const float CannonProjectileLifetimeMultiplier = 3f;
    }
}