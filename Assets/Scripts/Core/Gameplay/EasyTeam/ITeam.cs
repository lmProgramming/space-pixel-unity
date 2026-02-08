namespace Core.Gameplay.EasyTeam
{
    public interface ITeam
    {
        bool IsAllied(ITeam shipTeam);
        bool IsEnemy(ITeam shipTeam);
    }
}