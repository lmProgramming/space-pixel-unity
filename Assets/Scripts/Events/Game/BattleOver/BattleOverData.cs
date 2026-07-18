namespace Events.Game.BattleOver
{
    public enum BattleResult
    {
        FriendlyWin,
        EnemyWin
    }

    public struct BattleOverData
    {
        public BattleResult Result;
    }
}