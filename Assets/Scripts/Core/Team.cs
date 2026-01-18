using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class Team : MonoBehaviour
    {
        public bool treatNonAlliedAsEnemy = true;

        [SerializeField] private List<Team> allies = new();
        [SerializeField] private List<Team> enemies = new();

        public bool IsAllied(Team shipTeam)
        {
            return shipTeam == this || allies.Contains(shipTeam);
        }

        public bool IsEnemy(Team shipTeam)
        {
            var isAlly = IsAllied(shipTeam);
            if (isAlly) return false;

            var isEnemy = enemies.Contains(shipTeam);
            return isEnemy || treatNonAlliedAsEnemy;
        }
    }
}