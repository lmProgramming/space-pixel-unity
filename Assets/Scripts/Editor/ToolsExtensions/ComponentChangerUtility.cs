using Ships;
using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor.ToolsExtensions
{
    public static class ComponentChangerUtility
    {
        [MenuItem("CONTEXT/Module/Change to Engine")]
        private static void ChangeToEngine(MenuCommand command)
        {
            ChangeComponent<Engine>(command.context as Module);
        }

        [MenuItem("CONTEXT/Module/Change to Command")]
        private static void ChangeToCommand(MenuCommand command)
        {
            ChangeComponent<Command>(command.context as Module);
        }

        [MenuItem("CONTEXT/Module/Change to Cannon")]
        private static void ChangeToCannon(MenuCommand command)
        {
            ChangeComponent<Cannon>(command.context as Module);
        }

        [MenuItem("CONTEXT/Module/Change to Laser Beam")]
        private static void ChangeToLaserBeam(MenuCommand command)
        {
            ChangeComponent<LaserBeam>(command.context as Module);
        }

        [MenuItem("CONTEXT/Module/Change to Basic Module")]
        private static void ChangeToResource(MenuCommand command)
        {
            ChangeComponent<Basic>(command.context as Module);
        }

        [MenuItem("CONTEXT/Ship/Change to AI Ship")]
        private static void ChangeToAIShip(MenuCommand command)
        {
            ChangeShip<AIShip>(command.context as Ship);
        }

        [MenuItem("CONTEXT/Ship/Change to Player Ship")]
        private static void ChangeToPlayerShip(MenuCommand command)
        {
            ChangeShip<PlayerShip>(command.context as Ship);
        }

        private static void ChangeComponent<TNew>(Module oldComponent) where TNew : Module
        {
            if (oldComponent == null) return;

            var obj = oldComponent.gameObject;

            Undo.RegisterCompleteObjectUndo(obj, "Change Component");

            var oldValues = JsonUtility.ToJson(oldComponent);

            Undo.DestroyObjectImmediate(oldComponent);

            var newComponent = obj.AddComponent<TNew>();

            JsonUtility.FromJsonOverwrite(oldValues, newComponent);
        }

        private static void ChangeShip<TNew>(Ship oldShip) where TNew : Ship
        {
            if (oldShip == null) return;

            var obj = oldShip.gameObject;

            Undo.RegisterCompleteObjectUndo(obj, "Change Ship");

            var oldShipValues = JsonUtility.ToJson(oldShip);

            var shipCrewAssigner = obj.GetComponent<ShipCrewAssigner>();
            var oldShipCrewAssignerValues = JsonUtility.ToJson(shipCrewAssigner);
            if (shipCrewAssigner != null)
                Undo.DestroyObjectImmediate(shipCrewAssigner);

            Undo.DestroyObjectImmediate(oldShip);

            var newShipComponent = obj.AddComponent<TNew>();

            JsonUtility.FromJsonOverwrite(oldShipValues, newShipComponent);

            var newShipCrewAssigner = obj.GetComponent<ShipCrewAssigner>();
            if (newShipCrewAssigner == null) return;
            newShipCrewAssigner = obj.AddComponent<ShipCrewAssigner>();
            JsonUtility.FromJsonOverwrite(oldShipCrewAssignerValues, newShipCrewAssigner);
        }
    }
}