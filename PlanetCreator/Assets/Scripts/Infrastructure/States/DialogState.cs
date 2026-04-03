using UnityEngine;

namespace Infrastructure.States
{
    public class DialogState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;        
        private GameObject m_dialog;

        public DialogState(StateMachine stateMachine, GameObject dialog)
        {
            m_stateMachine = stateMachine;
            m_dialog = dialog;
        }

        public void Enter()
        {
            m_dialog.SetActive(true);
        }

        public void Exit()
        {
            m_stateMachine.ChangeState<MiniGameState>();
        }
    }
}