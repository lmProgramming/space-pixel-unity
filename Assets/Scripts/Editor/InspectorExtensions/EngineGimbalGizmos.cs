#if UNITY_EDITOR
using Ships.Modules;
using UnityEditor;
using UnityEngine;

namespace Editor.InspectorExtensions
{
    public static class EngineGimbalGizmos
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected)]
        private static void DrawEngineGimbalGizmos(Engine engine, GizmoType gizmoType)
        {
            if (!Application.isPlaying || !engine.drawGimbalGizmos) return;

            var thrustPoint = engine.WorldThrustPoint;
            var pivotForward = engine.transform.forward;
            var neutralDirection = engine.transform.up;
            var maxGimbalAngle = engine.MaxGimbalAngleForDebug;
            var arcRadius = engine.gizmoArcRadius;

            DrawGimbalLimitArc(thrustPoint, pivotForward, neutralDirection, maxGimbalAngle, arcRadius);
            DrawDirectionLine(thrustPoint, neutralDirection, arcRadius, new Color(0.75f, 0.75f, 0.75f, 0.55f), 1f);
            DrawDirectionLine(thrustPoint, GetGimbalDirection(engine.transform, engine.DesiredGimbalAngleForDebug),
                arcRadius, Color.yellow, 2f);

            if (Mathf.Abs(Mathf.DeltaAngle(engine.DesiredGimbalAngleForDebug, engine.CurrentThrusterAngleForDebug)) >
                0.5f)
                DrawDirectionLine(thrustPoint,
                    GetGimbalDirection(engine.transform, engine.CurrentThrusterAngleForDebug), arcRadius * 0.92f,
                    Color.green, 1.5f);

            DrawThrustArrow(thrustPoint, engine.WorldThrustDirection, engine.MaxThrust,
                engine.CurrentThrustRatioForTesting, engine.gizmoThrustUnitsPerNewton);
        }

        private static void DrawGimbalLimitArc(Vector3 center, Vector3 pivotForward, Vector3 neutralDirection,
            float maxGimbalAngle, float radius)
        {
            if (maxGimbalAngle <= Mathf.Epsilon) return;

            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            var arcStartDirection = RotateAroundPivot(neutralDirection, pivotForward, -maxGimbalAngle);
            Handles.DrawWireArc(center, pivotForward, arcStartDirection, maxGimbalAngle * 2f, radius);

            var minDirection = RotateAroundPivot(neutralDirection, pivotForward, -maxGimbalAngle);
            var maxDirection = RotateAroundPivot(neutralDirection, pivotForward, maxGimbalAngle);
            Handles.color = new Color(1f, 0.45f, 0.45f, 0.8f);
            Handles.DrawLine(center, center + minDirection * radius);
            Handles.DrawLine(center, center + maxDirection * radius);
        }

        private static void DrawDirectionLine(Vector3 origin, Vector2 direction, float length, Color color,
            float thickness)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            Handles.color = color;
            var end = origin + (Vector3)(direction.normalized * length);
            Handles.DrawAAPolyLine(thickness, origin, end);
            DrawArrowHead(end, direction, length * 0.12f, color, thickness);
        }

        private static void DrawThrustArrow(Vector3 origin, Vector2 direction, float maxThrust, float currentThrustRatio,
            float unitsPerNewton)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon || maxThrust <= Mathf.Epsilon) return;

            var normalizedDirection = direction.normalized;
            var maxLength = maxThrust * unitsPerNewton;
            var currentLength = maxLength * Mathf.Clamp01(currentThrustRatio);

            Handles.color = new Color(1f, 0.55f, 0.1f, 0.25f);
            DrawArrow(origin, normalizedDirection, maxLength, 2f);

            if (currentLength <= Mathf.Epsilon) return;

            Handles.color = new Color(1f, 0.35f, 0.05f, 0.95f);
            DrawArrow(origin, normalizedDirection, currentLength, 3f);
        }

        private static void DrawArrow(Vector3 origin, Vector2 direction, float length, float thickness)
        {
            var end = origin + (Vector3)(direction * length);
            Handles.DrawAAPolyLine(thickness, origin, end);
            DrawArrowHead(end, direction, length * 0.14f, Handles.color, thickness);
        }

        private static void DrawArrowHead(Vector3 tip, Vector2 direction, float size, Color color, float thickness)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            var forward = direction.normalized;
            var side = new Vector2(-forward.y, forward.x);
            var basePoint = tip - (Vector3)(forward * size);
            var left = basePoint + (Vector3)(side * size * 0.45f);
            var right = basePoint - (Vector3)(side * size * 0.45f);

            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, tip, left);
            Handles.DrawAAPolyLine(thickness, tip, right);
        }

        private static Vector2 GetGimbalDirection(Transform engineTransform, float gimbalAngle)
        {
            return (Quaternion.AngleAxis(gimbalAngle, engineTransform.forward) * engineTransform.up).normalized;
        }

        private static Vector3 RotateAroundPivot(Vector3 vector, Vector3 pivotForward, float angleDegrees)
        {
            return Quaternion.AngleAxis(angleDegrees, pivotForward) * vector;
        }
    }
}
#endif
