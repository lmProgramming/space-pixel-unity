using System;
using System.Collections.Generic;

namespace Core.Ships.Blueprints
{
    [Serializable]
    public class ShipBlueprint
    {
        public List<ModuleBlueprint> modules = new();
    }
}