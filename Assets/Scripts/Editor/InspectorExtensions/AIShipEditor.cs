using Ships;
using Ships.Modules;
using Ships.StateMachines.AIShip;
using UnityEditor;
using UnityEngine;

namespace Editor.InspectorExtensions
{
    [CustomEditor(typeof(AIShip))]
    public class AIShipEditor : UnityEditor.Editor
    {
        private const float DotRadius = 0.5f;
        private AIShipStateMachine _aiShipStateMachine;

        private void OnSceneGUI()
        {
            var aiShip = (AIShip)target;
            if (!aiShip || !(Module)aiShip.CommandModule) return;

            _aiShipStateMachine = aiShip.GetComponent<AIShipStateMachine>();

            DrawShipPosition(aiShip);

            DrawNavigationPath(aiShip);

            if (Application.isPlaying)
                SceneView.RepaintAll();
        }

        private static void DrawShipPosition(AIShip aiShip)
        {
            var position = aiShip.GetPosition();
            Handles.color = Color.red;
            Handles.DrawSolidDisc(position, Vector3.forward, DotRadius);
        }

        private static void DrawNavigationWaypoint(Vector2 waypoint, bool isCurrentTarget)
        {
            if (isCurrentTarget)
            {
                Handles.color = Color.cyan;
                Handles.DrawSolidDisc(waypoint, Vector3.forward, DotRadius * 1.4f);
            }
            else
            {
                Handles.color = Color.green;
                Handles.DrawSolidDisc(waypoint, Vector3.forward, DotRadius);
            }
        }

        private void DrawNavigationPath(AIShip aiShip)
        {
            if (!_aiShipStateMachine) return;

            var navigationHelper = _aiShipStateMachine.GetNavigationHelper();
            if (navigationHelper?.InternalPath == null || navigationHelper.InternalPath.Count == 0) return;

            var path = navigationHelper.InternalPath;
            var currentWaypointIndex = navigationHelper.InternalCurrentWaypointIndex;

            for (var i = 0; i < path.Count; i++)
            {
                var waypoint = (Vector2)path[i];
                DrawNavigationWaypoint(waypoint, i == currentWaypointIndex);

                DrawLineBetweenWaypoints(i);
            }

            DrawLineToCurrentWaypoint();

            return;

            void DrawLineBetweenWaypoints(int i)
            {
                if (i >= path.Count - 1) return;

                Handles.color = new Color(0f, 1f, 0f, 0.4f);
                Handles.DrawLine(path[i], path[i + 1]);
            }

            void DrawLineToCurrentWaypoint()
            {
                if (currentWaypointIndex >= path.Count) return;

                Handles.color = new Color(1f, 1f, 0f, 0.6f);
                Handles.DrawDottedLine(aiShip.GetPosition(), path[currentWaypointIndex], 4f);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}