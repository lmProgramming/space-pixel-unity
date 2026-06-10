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

        public const float ChanceOfSpawningExplosionOnDetachingConnectionPoint = 0.1f;
    }
}