using UnityEngine;

namespace Infrastructure.States
{
    public class DialogState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;        

        public DialogState(StateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            m_stateMachine.ChangeState<MiniGameState>();
        }
    }
}