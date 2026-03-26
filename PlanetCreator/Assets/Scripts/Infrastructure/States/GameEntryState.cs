using UnityEngine;

namespace Infrastructure.States
{
    public class GameEntryState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;

        public GameEntryState(StateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            m_stateMachine.ChangeState<DialogState>();
        }
    }
}
