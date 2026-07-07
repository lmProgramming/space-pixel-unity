using System;
using System.Collections.Generic;
using Core.Ships;
using LMPro;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable StringLiteralTypo

namespace Ships
{
    [RequireComponent(typeof(Ship))]
    public class ShipCrewAssigner : MonoBehaviour
    {
        [SerializeField] private List<CrewMember> crewMembers = new();

        [SerializeField]
        private bool generateMissingCrewMembers = true;

        private readonly string[] _firstNames =
        {
            "John", "Jane", "Alex", "Emily", "Michael", "Sarah", "David", "Laura", "Chris", "Anna", "James", "Olivia",
            "Robert", "Sophia", "Daniel", "Isabella", "Mark", "Mia", "Paul", "Amelia", "Andrew", "Charlotte", "Steven",
            "Evelyn", "Kevin", "Abigail", "Brian", "Madison", "Jason", "Elizabeth", "Sara", "Mikołaj"
        };

        private readonly string[] _lastNames =
        {
            "Smith", "Johnson", "Brown", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin",
            "Thompson", "Garcia", "Martinez", "Robinson", "Clark", "Rodriguez", "Lewis", "Lee", "Walker",
            "Hall", "Allen", "Young", "King", "Wright", "Scott", "Green", "Bibska", "Kubś"
        };

        private Ship _ship;

        private void Awake()
        {
            _ship = GetComponent<Ship>();
        }

        private void Start()
        {
            GenerateAndAssignCrewMembers();
        }

        public void GenerateAndAssignCrewMembers()
        {
            if (generateMissingCrewMembers) GenerateMissingCrewMembers();
            _ship.AssignCrewBySkill(crewMembers);
        }

        private void GenerateMissingCrewMembers()
        {
            var missingCrewToGenerate = _ship.CrewMissingCount - crewMembers.Count;
            for (var i = 0; i < missingCrewToGenerate; i++)
                crewMembers.Add(new CrewMember(
                    MathExt.RandomFrom(_firstNames), MathExt.RandomFrom(_lastNames),
                    Random.Range(20, 50),
                    GenerateSkills()));
        }

        private static Dictionary<CrewSkillType, int> GenerateSkills()
        {
            var skills = new Dictionary<CrewSkillType, int>();

            foreach (CrewSkillType skillType in Enum.GetValues(typeof(CrewSkillType)))
                skills[skillType] = Random.Range(0, 5);

            return skills;
        }
    }
}