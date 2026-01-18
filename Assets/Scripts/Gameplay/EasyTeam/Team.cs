using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using UnityEngine;

namespace Gameplay.EasyTeam
{
    public class Team : MonoBehaviour, ITeam
    {
        public bool treatNonAlliedAsEnemy = true;

        [SerializeField] private List<Team> allies = new();
        [SerializeField] private List<Team> enemies = new();

        public bool IsAllied(ITeam shipTeam)
        {
            return (Team)shipTeam == this || allies.Contains(shipTeam as Team);
        }

        public bool IsEnemy(ITeam shipTeam)
        {
            var isAlly = IsAllied(shipTeam);
            if (isAlly) return false;

            var isEnemy = enemies.Contains(shipTeam as Team);
            return isEnemy || treatNonAlliedAsEnemy;
        }
    }
}