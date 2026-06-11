using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Factories
{
    public struct TwoModuleShipResult
    {
        public Ship Ship { get; set; }
        public Command Command { get; set; }
        public Module Other { get; set; }

        public void Deconstruct(out Ship ship, out Command command, out Module other)
        {
            ship = Ship;
            command = Command;
            other = Other;
        }
    }

    public struct EngineThrustSpec
    {
        public Vector2 LocalPosition;
        public float LocalRotationZ;
        public float MaxThrust;
    }

    public struct ShipWithEnginesResult<T> where T : Ship
    {
        public T Ship { get; set; }
        public List<Engine> Engines { get; set; }
    }

    public struct CommandWithEngineResult
    {
        public Ship Ship { get; set; }
        public Command Command { get; set; }
        public Engine Engine { get; set; }

        public void Deconstruct(out Ship ship, out Command command, out Engine engine)
        {
            ship = Ship;
            command = Command;
            engine = Engine;
        }
    }

    public static class ShipTestFactory
    {
        private const int DefaultModulePixelSize = 5;
        private const float DefaultModuleSpacing = 5f;

        public static TwoModuleShipResult CreateTwoModuleShip(DiContainer container,
            ICollection<GameObject> createdObjects, int moduleWidth = DefaultModulePixelSize,
            int moduleHeight = DefaultModulePixelSize)
        {
            var layout = ShipTestBuilder.CreateShip(container, createdObjects)
                .WithCommand("Command", Vector2.zero, moduleWidth, moduleHeight)
                .WithModule("Module2", new Vector2(moduleWidth, 0), moduleWidth, moduleHeight)
                .BuildLayoutResult();

            return new TwoModuleShipResult
            {
                Ship = layout.Ship,
                Command = (Command)layout.CommandModule,
                Other = layout.OtherModules[0]
            };
        }

        public static Ship CreateShipWithCommandAndEngine(DiContainer container, ICollection<GameObject> createdObjects,
            Transform parent = null, float engineMaxThrust = 100f, float engineSpacing = DefaultModuleSpacing,
            int modulePixelSize = DefaultModulePixelSize)
        {
            var builder = ShipTestBuilder.CreateShip(container, createdObjects, "Ship");
            if (parent != null)
                builder.ParentedTo(parent);

            return builder
                .WithCommand("Command", Vector2.zero, modulePixelSize, modulePixelSize)
                .WithEngineModule(new Vector2(engineSpacing, 0f), engineMaxThrust, modulePixelSize, modulePixelSize)
                .Build(initializeModules: true);
        }
    }
}
