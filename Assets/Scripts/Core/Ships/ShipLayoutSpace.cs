using System;
using Core.Ships.Module;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Ships
{
    /// <summary>
    ///     Module layout coordinates are relative to the command module transform.
    ///     The command module is always at layout origin (0, 0, 0) with identity rotation.
    /// </summary>
    public static class ShipLayoutSpace
    {
        public static Transform GetOrigin(IShip ship)
        {
            if (ship == null) throw new ArgumentNullException(nameof(ship));

            var commandModule = ship.CommandModule;
            if (HasAliveTransform(commandModule))
                return commandModule.Transform;

            if (ship is Component shipComponent)
                return shipComponent.transform;

            throw new UnityException("[ShipLayoutSpace] Ship has no layout origin.");
        }

        public static Vector3 WorldToLocal(IShip ship, Vector3 world)
        {
            return GetOrigin(ship).InverseTransformPoint(world);
        }

        public static Vector3 LocalToWorld(IShip ship, Vector3 local)
        {
            return GetOrigin(ship).TransformPoint(local);
        }

        public static Quaternion WorldToLocalRotation(IShip ship, Quaternion world)
        {
            return Quaternion.Inverse(GetOrigin(ship).rotation) * world;
        }

        public static Quaternion LocalToWorldRotation(IShip ship, Quaternion local)
        {
            return GetOrigin(ship).rotation * local;
        }

        public static bool IsCommandModule(IShip ship, IModule module)
        {
            return HasAliveTransform(module) && ship.CommandModule == module;
        }

        public static void ApplyLayoutTransform(IShip ship, Transform moduleTransform, Vector3 layoutPosition,
            Quaternion layoutRotation)
        {
            if (!moduleTransform)
                throw new ArgumentNullException(nameof(moduleTransform));

            var commandModule = ship.CommandModule;
            if (HasAliveTransform(commandModule) && commandModule.Transform == moduleTransform)
            {
                moduleTransform.localPosition = Vector3.zero;
                moduleTransform.localRotation = Quaternion.identity;
                return;
            }

            var origin = GetOrigin(ship);
            moduleTransform.SetPositionAndRotation(
                origin.TransformPoint(layoutPosition),
                origin.rotation * layoutRotation);
        }

        private static bool HasAliveTransform(IModule module)
        {
            // Plain null-coalescing cannot detect destroyed Unity objects, and even a destroyed
            // module passes an interface-typed null check, so cast to Object first.
            return module is Object unityObject &&
                   unityObject != null &&
                   unityObject.transform != null;
        }
    }
}