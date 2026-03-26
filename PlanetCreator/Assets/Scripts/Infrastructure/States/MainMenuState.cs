using Cinemachine;
using UnityEngine;

namespace Infrastructure.States
{
    public class MainMenuState : MonoBehaviour, IState
    {
        [SerializeField] private CinemachineVirtualCamera m_camera;
        [SerializeField] private GameObject m_UIPanel;

        private StateMachine m_stateMachine;

        public MainMenuState(StateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        public void Enter()
        {

        }

        public void Exit()
        {
            m_stateMachine.ChangeState<GameEntryState>();
        }
    }
}
