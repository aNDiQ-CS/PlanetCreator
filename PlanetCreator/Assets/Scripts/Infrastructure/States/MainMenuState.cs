using UnityEngine;

namespace Infrastructure.States
{
    public class MainMenuState : IState
    {
        private readonly GameObject m_uiPanel;

        public MainMenuState(GameObject uiPanel)
        {
            m_uiPanel = uiPanel;
        }

        public void Enter()
        {
            if (m_uiPanel != null)
                m_uiPanel.SetActive(true);
        }

        public void Exit()
        {
            if (m_uiPanel != null)
                m_uiPanel.SetActive(false);
        }
    }
}