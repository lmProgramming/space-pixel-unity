using System;
using System.Collections.Generic;

namespace Ships.Serialization
{
    [Serializable]
    public class ShipSnapshot
    {
        public string shipName;
        public List<ModuleSnapshot> modules = new();
        public List<ModuleConnection> connections = new();
        public int commandModuleIndex;

        public ShipSnapshot()
        {
        }

        public ShipSnapshot(string name)
        {
            shipName = name;
        }
    }
}