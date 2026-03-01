using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Ship
{
    [Serializable]
    public class CrewMember
    {
        [field: SerializeField]
        public bool IsAlive { get; private set; }

        [field: SerializeField]
        public string FirstName { get; private set; }

        [field: SerializeField]
        public string LastName { get; private set; }

        [field: SerializeField]
        public int Age { get; private set; }

        private readonly Dictionary<CrewSkillType, int> _skills;

        public CrewMember(string firstName, string lastName, int age,
            Dictionary<CrewSkillType, int> skills = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            _skills = skills ?? new Dictionary<CrewSkillType, int>();
            IsAlive = true;
        }

        public event Action<CrewMember> OnDied;

        public int GetSkillLevel(CrewSkillType skillType)
        {
            return _skills.GetValueOrDefault(skillType, 0);
        }

        public void Kill()
        {
            if (!IsAlive) return;
            IsAlive = false;
            OnDied?.Invoke(this);
        }
    }
}