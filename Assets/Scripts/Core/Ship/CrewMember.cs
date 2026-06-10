using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LMPro.DataStructures;
using UnityEngine;

[assembly: InternalsVisibleTo("Ships.Tests")]

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

        [SerializeField]
        private SerializableDictionary<CrewSkillType, int> skills;

        public CrewMember(string firstName, string lastName, int age,
            Dictionary<CrewSkillType, int> skills = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            this.skills = skills != null
                ? new SerializableDictionary<CrewSkillType, int>(skills)
                : new SerializableDictionary<CrewSkillType, int>();
            IsAlive = true;
        }

        public event Action<CrewMember> OnDied;

        public int GetSkillLevel(CrewSkillType skillType)
        {
            if (skills == null) Debug.LogWarning("[CrewMember] Skills not assigned");
            return skills?.GetValueOrDefault(skillType, 0) ?? 0;
        }

        public void Kill()
        {
            if (!IsAlive) return;
            IsAlive = false;
            OnDied?.Invoke(this);
        }

#if UNITY_INCLUDE_TESTS
        internal int OnDiedSubscriberCountForTesting =>
            OnDied?.GetInvocationList().Length ?? 0;
#endif
    }
}