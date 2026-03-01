using System;
using Core.Gameplay.EasyTeam;
using Core.Ship;
using LM.Graph;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public class MockShip : IShip
    {
        private readonly Vector2 _position;

        public MockShip(ITeam team, Vector2 position)
        {
            Team = team;
            _position = position;
        }

        public ITeam Team { get; }
        public IModule CommandModule => null;
        public Collider2D[] OwnColliders => Array.Empty<Collider2D>();
        public float GeneralEfficiency => 1;
        public Graph<IModule> ModuleGraph => null;
        public Vector2 AttackTargetPosition => Vector2.zero;
        public float CaptainMultiplier => 1;

        public Vector2 GetPosition()
        {
            return _position;
        }

        public void OnModuleDestroyed(IModule module)
        {
            throw new NotImplementedException();
        }
    }
}