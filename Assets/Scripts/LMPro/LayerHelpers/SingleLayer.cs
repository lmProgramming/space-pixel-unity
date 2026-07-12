using System;
using UnityEngine;

namespace LMPro.LayerHelpers
{
    [Serializable]
    public struct SingleLayer
    {
        [SerializeField]
        private int layerIndex;

        public int LayerIndex
        {
            get => layerIndex;
            set
            {
                if (value is > 0 and < 32) layerIndex = value;
            }
        }

        public int Mask => 1 << layerIndex;

        public static implicit operator int(SingleLayer layer)
        {
            return layer.layerIndex;
        }

        public static implicit operator SingleLayer(int layer)
        {
            return new SingleLayer { layerIndex = layer };
        }
    }
}