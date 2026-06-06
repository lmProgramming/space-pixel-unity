using System.Collections.Generic;
using AI.EasyState;
using Core.Services;
using JetBrains.Annotations;
using Ships.StateMachines.AIShip.States;
using UnityEngine;
using Zenject;

namespace Ships.StateMachines.AIShip
{
    public class AIShipStateMachine : StateMachine<AIShipStateMachine, AIShipState>
    {
        [Inject] public INavigationService NavigationService { get; private set; }
        public bool ShouldMove { get; private set; }
        public Vector2? Target { get; private set; }

        protected override string DefaultState => "Lookout";

        protected override Dictionary<string, float> CreateWeightedStates()
        {
            return new Dictionary<string, float>
            {
                { "Lookout", 1f }
            };
        }

        public void SetMovementTarget(Vector2 target)
        {
            Target = target;
            ShouldMove = true;
        }

        public void ClearMovementTarget()
        {
            Target = null;
            ShouldMove = false;
        }

        [CanBeNull]
        internal NavigationFollower GetNavigationHelper()
        {
            return (CurrentState as AIShipState)?.NavigationFollower;
        }
    }
}