using UnityEngine;

namespace Framework.Core.StateMachine
{
    public class StateMachine
    {
        public IState CurrentState { get; private set; }

        public void Initialize(IState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void TransitionTo(IState nextState)
        {
            if (CurrentState != null)
            {
                CurrentState.Exit();
            }
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }

        public void LateUpdate()
        {
            CurrentState?.LateUpdate();
        }
    }

    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void LateUpdate();
        void Exit();
    }

    public abstract class State : IState
    {
        protected StateMachine stateMachine;
        protected GameObject owner;

        public State(StateMachine stateMachine, GameObject owner = null)
        {
            this.stateMachine = stateMachine;
            this.owner = owner;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
        public virtual void Exit() { }
    }
}
