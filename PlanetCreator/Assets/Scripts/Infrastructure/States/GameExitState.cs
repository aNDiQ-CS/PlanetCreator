using UnityEngine;

namespace Infrastructure.States
{
    public class GameExitState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;

        public GameExitState(StateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            m_stateMachine.ChangeState<MainMenuState>();
        }
    }
}
