using UnityEngine;

namespace Core.Constants
{
    [CreateAssetMenu(fileName = "ProgressionConstants", menuName = "Constants/ProgressionConstants")]
    public class ProgressionConstants : ScriptableObject
    {
        public int creditsPerRepairedPixel = 1;

        public int repairedPixelsPerFrame = 1;
        public int easyBattleCreditsReward = 50;
        public int mediumBattleCreditsReward = 100;
        public int bossBattleCreditsReward = 200;
        public int initialCredits;
    }
}