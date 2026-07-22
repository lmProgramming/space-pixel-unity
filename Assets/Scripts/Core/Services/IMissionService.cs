using System;

namespace Core.Services
{
    public interface IMissionService
    {
        event Action OnVictory;
        event Action OnDefeat;
        void Setup();
    }
}