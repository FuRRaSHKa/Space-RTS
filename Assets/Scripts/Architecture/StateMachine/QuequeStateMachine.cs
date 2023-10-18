using System;
using System.Collections.Generic;
using System.Linq;

namespace HalloGames.Architecture.StateMachine
{
    public class QuequeStateMachine : StateMachine
    {
        private Type _firstState;
        private Type _lastState;

        public void InitStates(Dictionary<Type, IState> states, Type firstState, Type lastState)
        {
            InitStates(states);
            _firstState = firstState;
            _lastState = lastState;
        }

        public void StartMachine()
        {
            ChangeState(_firstState);
        }

        public void NextState()
        {
            IState targetState = _states.Values.SkipWhile(k => k != _currentState).Skip(1).DefaultIfEmpty(_states[_firstState]).FirstOrDefault();
            ChangeState(targetState);
        }

        public void PrevState()
        {
            IState targetState = _states.Values.TakeWhile(k => k != _currentState).DefaultIfEmpty(_states[_lastState]).LastOrDefault();
            ChangeState(targetState);
        }

        public void Dispose()
        {
            _currentState?.Exit();
            _currentState = null;
        }
    }

    public class StateMachine
    {
        protected Dictionary<Type, IState> _states;

        protected IState _currentState;

        public event Action<string> OnStateChange;

        public Type CurrentState => _currentState.GetType();

        public void InitStates(Dictionary<Type, IState> states)
        {
            _states = states;
        }

        public void ChangeState(Type type)
        {
            _currentState?.Exit();
            _currentState = _states[type];

            OnStateChange?.Invoke(type.ToString());
            _currentState?.Enter();
        }

        protected void ChangeState(IState state)
        {
            _currentState?.Exit();
            _currentState = state;

            OnStateChange?.Invoke(state.GetType().ToString());
            _currentState?.Enter();
        }
    }
}