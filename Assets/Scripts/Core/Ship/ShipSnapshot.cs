using System;
using System.Collections.Generic;

namespace Core.Ship
{
    [Serializable]
    public class ShipSnapshot
    {
        public string shipName;
        public List<ModuleSnapshot> modules = new();
        public int commandModuleIndex;
        public List<ModuleConnection> connections = new();

        public ShipSnapshot()
        {
        }

        public ShipSnapshot(string name)
        {
            shipName = name;
        }
    }
}