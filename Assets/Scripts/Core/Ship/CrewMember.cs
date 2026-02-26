using System;
using System.Collections.Generic;

namespace Core.Ship
{
    [Serializable]
    public class CrewMember
    {
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

        public string FirstName { get; }
        public string LastName { get; }
        public int Age { get; }
        public bool IsAlive { get; private set; }

        public event Action<CrewMember> OnDied;

        public int GetSkillLevel(CrewSkillType skillType)
        {
            return _skills.TryGetValue(skillType, out var level) ? level : 0;
        }

        public void Kill()
        {
            if (!IsAlive) return;
            IsAlive = false;
            OnDied?.Invoke(this);
        }
    }
}
