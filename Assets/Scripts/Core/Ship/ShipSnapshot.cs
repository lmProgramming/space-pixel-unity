using System;
using System.Collections.Generic;
using Core.Ship.Snapshots.Module;

namespace Core.Ship
{
    [Serializable]
    public class ShipSnapshot
    {
        public int schemaVersion = 2;
        public string shipName;
        public string commandModuleInstanceId;
        public List<ModuleSnapshot> modules = new();

        public ShipSnapshot()
        {
        }

        public ShipSnapshot(string name)
        {
            shipName = name;
        }
    }
}