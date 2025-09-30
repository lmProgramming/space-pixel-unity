using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ComponentChangerUtility
    {
        [MenuItem("CONTEXT/Module/Change to Engine")]
        private static void ChangeToEngine(MenuCommand command)
        {
            ChangeComponent<Engine>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Command")]
        private static void ChangeToCommand(MenuCommand command)
        {
            ChangeComponent<Command>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Cannon")]
        private static void ChangeToCannon(MenuCommand command)
        {
            ChangeComponent<Cannon>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Laser Beam")]
        private static void ChangeToLaserBeam(MenuCommand command)
        {
            ChangeComponent<LaserBeam>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Basic Module")]
        private static void ChangeToModule(MenuCommand command)
        {
            ChangeComponent<Module>(command.context as MonoBehaviour);
        }

        private static void ChangeComponent<TNew>(MonoBehaviour oldComponent) where TNew : MonoBehaviour
        {
            if (oldComponent == null) return;

            var obj = oldComponent.gameObject;

            Undo.RegisterCompleteObjectUndo(obj, "Change Component");

            var oldValues = JsonUtility.ToJson(oldComponent);

            Object.DestroyImmediate(oldComponent);

            var newComponent = obj.AddComponent<TNew>();

            JsonUtility.FromJsonOverwrite(oldValues, newComponent);
        }
    }
}