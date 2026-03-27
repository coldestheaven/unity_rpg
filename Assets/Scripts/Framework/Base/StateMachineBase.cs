using System;
using Framework.Core.StateMachine;

namespace Framework.Base
{
    public abstract class StateMachineBase<TState> : MonoBehaviourBase where TState : Enum
    {
        private readonly StateMachine stateMachine = new StateMachine();

        public event Action<TState, TState> OnStateChanged;

        public TState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState.ToString();

        protected void InitializeState(IState startingState, TState startingStateType)
        {
            CurrentState = startingStateType;
            stateMachine.Initialize(startingState);
        }

        protected void ChangeState(IState nextState, TState nextStateType)
        {
            TState previousState = CurrentState;
            CurrentState = nextStateType;

            if (stateMachine.CurrentState == null)
            {
                stateMachine.Initialize(nextState);
            }
            else
            {
                stateMachine.TransitionTo(nextState);
            }

            OnStateChanged?.Invoke(previousState, nextStateType);
        }

        protected override void Update()
        {
            base.Update();
            stateMachine.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            stateMachine.FixedUpdate();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            stateMachine.LateUpdate();
        }
    }
}
