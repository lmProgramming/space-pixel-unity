using System;
using UnityEngine;

namespace Core.Ship
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

        [SerializeReference]
        public PixelGridSnapshot colorGrid;

        [SerializeReference]
        public ArmorGridSnapshot armorGrid;

        [SerializeReference]
        public HealthGridSnapshot healthGrid;
    }
}