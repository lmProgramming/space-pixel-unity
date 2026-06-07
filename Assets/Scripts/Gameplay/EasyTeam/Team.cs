using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gameplay.EasyTeam
{
    public class Team : MonoBehaviour, ITeam
    {
        public bool treatNonAlliedAsEnemy = true;

        [SerializeField] private List<Team> allies = new();
        [SerializeField] private List<Team> enemies = new();

        private string _layerName;
        public int Layer { get; private set; }

        public bool IsAllied(ITeam shipTeam)
        {
            return (Team)shipTeam == this || allies.Contains((Team)shipTeam);
        }

        public bool IsEnemy(ITeam shipTeam)
        {
            var isAlly = IsAllied(shipTeam);
            if (isAlly) return false;

            var isEnemy = enemies.Contains(shipTeam as Team);
            return isEnemy || treatNonAlliedAsEnemy;
        }

        public void SetLayerName(string layerName)
        {
            _layerName = layerName;
            Layer = LayerMask.NameToLayer(_layerName);
            Assert.IsTrue(Layer != -1, $"Layer '{_layerName}' does not exist. Please create it in the Unity editor.");
        }
    }
}