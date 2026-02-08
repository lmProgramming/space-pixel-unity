using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.EasyState.States
{
    [Serializable]
    public abstract class BaseState : IState
    {
        protected float TimeInState;
        public abstract string StateName { get; }
        public bool OverridableByForce { get; private set; } = true;

        public virtual void Enter(StateMachine stateMachine, IStateData data)
        {
            TimeInState = 0f;
            Debug.Log($"Entering state: {StateName}");
        }

        public virtual void Update(StateMachine stateMachine, float deltaTime)
        {
            TimeInState += deltaTime;
        }

        public virtual void Exit(StateMachine stateMachine)
        {
            Debug.Log($"Exiting state: {StateName}");
        }

        public virtual bool CanTransitionTo(string stateName)
        {
            return true;
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