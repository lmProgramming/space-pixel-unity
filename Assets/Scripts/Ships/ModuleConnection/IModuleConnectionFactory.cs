using Core.Ships;
using UnityEngine;

namespace Ships.ModuleConnection
{
    public interface IModuleConnectionFactory
    {
        void ConnectModules(IShip ship, Transform shipTransform);
    }
}