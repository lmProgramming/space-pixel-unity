using System;
using Core.Ship;
using Core.Ship.Snapshots.PixelatedRigidbody;
using Pixelation;
using Ships.Systems.Gimbal;
using UnityEngine;

namespace Ships.Snapshot
{
    public static class NestedPixelatedRigidbodyFactory
    {
        public static GameObject CreateShell(Transform parent, PixelatedRigidbodySnapshot snapshot)
        {
            if (snapshot == null)
                throw new UnityException("[NestedPixelatedRigidbodyFactory] Snapshot is null.");

            var childName = string.IsNullOrWhiteSpace(snapshot.name)
                ? snapshot.rigidbodyType.ToString()
                : snapshot.name;

            var childGo = new GameObject(childName);
            childGo.transform.SetParent(parent, false);
            childGo.transform.localPosition = snapshot.localPosition;
            childGo.transform.localRotation = snapshot.localRotation;

            childGo.AddComponent<SpriteRenderer>();

            var rigidbody = childGo.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.bodyType = snapshot.rigidbodyType == PixelatedRigidbodyType.Nozzle
                ? RigidbodyType2D.Kinematic
                : RigidbodyType2D.Dynamic;

            childGo.AddComponent<PolygonCollider2D>();

            childGo.AddComponent(ResolveComponentType(snapshot.rigidbodyType));

            return childGo;
        }

        private static Type ResolveComponentType(PixelatedRigidbodyType rigidbodyType)
        {
            return rigidbodyType switch
            {
                PixelatedRigidbodyType.Nozzle => typeof(Nozzle),
                PixelatedRigidbodyType.PixelatedRigidbody => typeof(PixelatedRigidbody),
                _ => throw new UnityException(
                    $"[NestedPixelatedRigidbodyFactory] Unknown rigidbody type '{rigidbodyType}'.")
            };
        }
    }
}