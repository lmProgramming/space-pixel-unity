using Ship.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class ComponentChangerUtility
    {
        [MenuItem("CONTEXT/Module/Change to Engine")]
        private static void ChangeToEngine1(MenuCommand command)
        {
            ChangeComponent<Module, Engine>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Command")]
        private static void ChangeToCommand1(MenuCommand command)
        {
            ChangeComponent<Module, Command>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Cannon")]
        private static void ChangeToCannon1(MenuCommand command)
        {
            ChangeComponent<Module, Cannon>(command.context as MonoBehaviour);
        }

        [MenuItem("CONTEXT/Module/Change to Basic Module")]
        private static void ChangeToModule1(MenuCommand command)
        {
            ChangeComponent<Module, Module>(command.context as MonoBehaviour);
        }

        private static void ChangeComponent<TBase, TNew>(MonoBehaviour oldComponent)
            where TBase : MonoBehaviour where TNew : MonoBehaviour
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