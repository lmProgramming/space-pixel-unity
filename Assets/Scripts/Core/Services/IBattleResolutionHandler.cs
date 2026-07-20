namespace Core.Services
{
    public interface IBattleResolutionHandler
    {
        void OnBattleVictory();

        void OnBattleDefeat();
    }
}