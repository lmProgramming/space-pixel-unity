using System;
using System.Collections.Generic;

namespace Core.Ship
{
    [Serializable]
    public class ShipSnapshot
    {
        public int schemaVersion = 1;
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