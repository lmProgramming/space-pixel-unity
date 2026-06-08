using System;
using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using LMPro.LayerHelpers;
using UnityEngine;

namespace Gameplay.EasyTeam
{
    public class Team : MonoBehaviour, ITeam
    {
        public bool treatNonAlliedAsEnemy = true;

        [SerializeField] private List<Team> allies = new();
        [SerializeField] private List<Team> enemies = new();

        [field: SerializeField]
        public SingleLayer Layer { get; private set; }

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

        public void SetLayerName(int newLayer)
        {
            if (newLayer == -1) throw new Exception("Invalid layer name - was \"-1\"");
            Layer = newLayer;
        }
    }
}