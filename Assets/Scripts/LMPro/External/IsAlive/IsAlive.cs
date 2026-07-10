using UnityEngine;

namespace LMPro.External.IsAlive
{
    public interface IHasAliveCheck
    {
    }

    public static class UnityGameObjectAliveExtension
    {
        public static override bool Equals(this IHasAliveCheck aObj, object obj)
        {
            if (aObj is Object o)
                return o.Equals(obj);
            return aObj.Equals(obj);
        }
        
        public static bool IsAlive(this IHasAliveCheck aObj)
        {
            if (aObj is Object o)
                return o != null;
            return aObj != null;
        }

        // useful to run when GameObject is usually enabled and being disabled means it might be BEING destroyed
        public static bool IsAliveEnabled(this IHasAliveCheck aObj)
        {
            return aObj switch
            {
                Component c => c != null && c.gameObject.activeInHierarchy,
                Object o => o != null,
                _ => aObj != null
            };
        }
    }
}