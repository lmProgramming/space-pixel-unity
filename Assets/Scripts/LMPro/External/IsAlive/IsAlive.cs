using UnityEngine;

namespace LMPro.External.IsAlive
{
    public interface IHasAliveCheck
    {
    }

    public static class UnityObjectAliveExtension
    {
        public static bool IsAlive(this IHasAliveCheck aObj)
        {
            if (aObj is Object o)
                return o != null;
            return aObj != null;
        }
    }
}