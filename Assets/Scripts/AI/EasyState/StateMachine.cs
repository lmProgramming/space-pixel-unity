using System;
using System.Collections.Generic;
using AI.EasyState.States;
using JetBrains.Annotations;
using UnityEngine;
using ZLinq;
using Random = UnityEngine.Random;

namespace AI.EasyState
{
    public abstract class StateMachine<TSelf, TStateBase> : MonoBehaviour
        where TSelf : StateMachine<TSelf, TStateBase>
        where TStateBase : BaseState<TSelf, TStateBase>
    {
        private readonly Dictionary<string, BaseState<TSelf, TStateBase>> _states = new();

        private Dictionary<string, float> _weightedStates;

        protected abstract string DefaultState { get; }

        protected BaseState<TSelf, TStateBase> CurrentState { get; private set; }
        public IAgent Controller { get; private set; }
        public bool UseManualUpdate { get; set; }
        private TSelf Self => (TSelf)this;

        private void Awake()
        {
            _weightedStates = CreateWeightedStates();

            Controller = GetComponent<IAgent>();
        }

        private void Update()
        {
            if (UseManualUpdate) return;
            Tick(Time.deltaTime);
        }

        private void OnEnable()
        {
            OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            OnStateChanged -= HandleStateChange;
        }

        private void HandleStateChange(BaseState<TSelf, TStateBase> state)
        {
            currentStateNameDebug = state.StateName;
            currentStateDataDebug = state.DebugInfo();
        }

        public event Action<BaseState<TSelf, TStateBase>> OnStateChanged;

        protected virtual Dictionary<string, float> CreateWeightedStates()
        {
            return new Dictionary<string, float>();
        }

        public void RegisterState(BaseState<TSelf, TStateBase> state, float? weight = null)
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

        public void TransitionToState(string stateName, [CanBeNull] IStateData data = null)
        {
            if (!_states.TryGetValue(stateName, out var newState))
            {
                Debug.LogWarning($"State {stateName} not found in state machine");
                return;
            }

            if (!CurrentState.CanTransitionTo(stateName))
            {
                Debug.LogWarning($"Cannot transition from {CurrentState.StateName} to {stateName}");
                return;
            }

            CurrentState.Exit(Self);
            CurrentState = newState;

            CurrentState.Enter(Self, data);

            OnStateChanged?.Invoke(CurrentState);
        }

        public void ForceTransitionToState(string stateName, IStateData data = null)
        {
            if (!_states.TryGetValue(stateName, out var newState))
            {
                Debug.LogWarning($"State {stateName} not found in state machine");
                return;
            }

            if (!CurrentState.OverridableByForce) return;

            CurrentState.Exit(Self);
            CurrentState = newState;

            CurrentState.Enter(Self, data);

            OnStateChanged?.Invoke(CurrentState);
        }

        public void Tick(float deltaTime)
        {
            CurrentState.Update(Self, deltaTime);
        }

        public void StartStateMachine([CanBeNull] string initialStateName = null, IStateData data = null)
        {
            initialStateName ??= DefaultState;

            if (!_states.TryGetValue(initialStateName, out var newState))
                Debug.LogWarning($"State {initialStateName} not found in state machine");

            CurrentState = newState;

            CurrentState!.Enter(Self, data);
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
                ? _weightedStates.Values.AsValueEnumerable().Sum()
                : _weightedStates.AsValueEnumerable().Where(kv => kv.Key != skippedState).Sum(kv => kv.Value);
        }

        private string CalculateNextState([CanBeNull] string skippedState = null)
        {
            var val = Random.value * GetTotalStateWeight(skippedState);
            var possibleStates = skippedState == null
                ? _weightedStates
                : _weightedStates.AsValueEnumerable().Where(kv => kv.Key != skippedState)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

            var cumulative = 0f;
            foreach (var stateWeight in possibleStates.AsValueEnumerable().SkipLast(1))
            {
                cumulative += stateWeight.Value;
                if (val <= cumulative) return stateWeight.Key;
            }

            return possibleStates.Keys.AsValueEnumerable().Last();
        }

        public T GetState<T>(string stateName) where T : BaseState<TSelf, TStateBase>
        {
            return _states.TryGetValue(stateName, out var state) ? state as T : null;
        }

        // ReSharper disable NotAccessedField.Local
        [SerializeField] private string currentStateNameDebug;

        [SerializeField] private string currentStateDataDebug;
        // ReSharper restore NotAccessedField.Local
    }
}