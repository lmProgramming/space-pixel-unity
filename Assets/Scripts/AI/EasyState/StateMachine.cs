using System.Collections.Generic;
using System.Linq;
using AI.EasyState.States;
using JetBrains.Annotations;
using Ships;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.EasyState
{
    public class StateMachine : MonoBehaviour
    {
        private readonly Dictionary<string, IState> _states = new();

        private readonly Dictionary<string, float> _weightedStates = new()
        {
            { "Idle", 1f }
        };

        private IState CurrentState { get; set; }

        public Ship ShipController { get; private set; }

        private static string DefaultState => "Idle";

        private void Awake()
        {
            ShipController = GetComponent<Ship>();
        }

        private void Update()
        {
            CurrentState?.Update(this, Time.deltaTime);
        }

        public void RegisterState(IState state, float? weight = null)
        {
            if (weight.HasValue) AddWeightedState(state.StateName, weight.Value);

            _states.TryAdd(state.StateName, state);
        }

        private void AddWeightedState(string stateName, float weight)
        {
            _weightedStates[stateName] = weight;
        }

        public void RemoveWeightedState(string stateName)
        {
            _weightedStates.Remove(stateName);
        }

        private void TransitionToState(string stateName)
        {
            if (!_states.TryGetValue(stateName, out var newState))
            {
                Debug.LogWarning($"State {stateName} not found in state machine");
                return;
            }

            if (CurrentState != null && !CurrentState.CanTransitionTo(stateName))
            {
                Debug.LogWarning($"Cannot transition from {CurrentState.StateName} to {stateName}");
                return;
            }

            CurrentState?.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }

        public void ForceTransitionToState(string stateName)
        {
            if (!_states.TryGetValue(stateName, out var newState))
            {
                Debug.LogWarning($"State {stateName} not found in state machine");
                return;
            }

            if (!CurrentState.OverridableByForce) return;

            CurrentState?.Exit(this);
            CurrentState = newState;
            CurrentState.Enter(this);
        }

        public void StartStateMachine([CanBeNull] string initialStateName = null)
        {
            initialStateName ??= DefaultState;

            TransitionToState(initialStateName);
        }

        public void TransitionToDefaultState()
        {
            TransitionToState(DefaultState);
        }

        public void TransitionToNextState([CanBeNull] string skippedState = null)
        {
            var nextState = CalculateNextState(skippedState);
            TransitionToState(nextState);
        }

        private float GetTotalStateWeight([CanBeNull] string skippedState = null)
        {
            return skippedState == null
                ? _weightedStates.Values.Sum()
                : _weightedStates.Where(kv => kv.Key != skippedState).Sum(kv => kv.Value);
        }

        private string CalculateNextState([CanBeNull] string skippedState = null)
        {
            var val = Random.value * GetTotalStateWeight(skippedState);
            var possibleStates = skippedState == null
                ? _weightedStates
                : _weightedStates.Where(kv => kv.Key != skippedState).ToDictionary(kv => kv.Key, kv => kv.Value);

            var cumulative = 0f;
            foreach (var stateWeight in possibleStates.SkipLast(1))
            {
                cumulative += stateWeight.Value;
                if (val <= cumulative) return stateWeight.Key;
            }

            return possibleStates.Keys.Last();
        }


        public T GetState<T>(string stateName) where T : class, IState
        {
            return _states.TryGetValue(stateName, out var state) ? state as T : null;
        }
    }
}