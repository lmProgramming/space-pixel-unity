using System;
using Core.Ships.Snapshots.PixelatedRigidbody.Internals;
using UnityEngine;

namespace Core.Ships.Snapshots.PixelatedRigidbody
{
    [Serializable]
    public class PixelatedRigidbodySnapshot
    {
        public string name;
        public PixelatedRigidbodyType rigidbodyType;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public float defaultPixelHealth = 1f;
        public float maxArmorHealth = 10f;
        public int startPixelCount;

        public int spriteRenderedOrderInLayer;
        public int spriteRenderedSortingLayerID;

        [SerializeReference]
        public PixelGridSnapshot colorGrid;

        [SerializeReference]
        public ArmorGridSnapshot armorGrid;

        [SerializeReference]
        public HealthGridSnapshot healthGrid;

        public string typePayloadJson;
    }
}