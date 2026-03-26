using UnityEngine;

namespace Infrastructure.States
{
    public class MiniGameState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;

        public MiniGameState(StateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            m_stateMachine.ChangeState<DialogState>();
            m_stateMachine.ChangeState<GameExitState>();
        }
    }
}
