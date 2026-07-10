using LMPro.External.IsAlive;

namespace Core.Ships
{
    public interface ISAS : IHasAliveCheck
    {
        bool IsSASOn { get; }
        void ToggleSAS();
    }
}