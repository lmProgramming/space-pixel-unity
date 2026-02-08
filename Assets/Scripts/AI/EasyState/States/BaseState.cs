using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.EasyState.States
{
    [Serializable]
    public abstract class BaseState<TStateMachine, TStateBase>
        where TStateMachine :
        StateMachine<TStateMachine, TStateBase>
        where TStateBase : BaseState<TStateMachine, TStateBase>
    {
        protected float TimeInState;
        public abstract string StateName { get; }
        public bool OverridableByForce { get; private set; } = true;

        public abstract bool CanTransitionTo(string stateName);

        public virtual void Enter(TStateMachine stateMachine, IStateData data)
        {
            TimeInState = 0f;
            Debug.Log($"Entering state: {StateName}");
        }

        public virtual void Update(TStateMachine stateMachine, float deltaTime)
        {
            TimeInState += deltaTime;
        }

        public virtual void Exit(TStateMachine stateMachine)
        {
            Debug.Log($"Exiting state: {StateName}");
        }

        private static Vector2 GetRandomDirection()
        {
            return Random.insideUnitCircle.normalized;
        }

        protected static Vector2 GetRandomDirectionBiased(Vector2 currentDirection, float bias)
        {
            var random = GetRandomDirection();
            return Vector2.Lerp(random, currentDirection, bias).normalized;
        }
    }
}