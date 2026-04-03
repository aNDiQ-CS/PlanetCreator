using Infrastructure.States;
using UnityEngine;

namespace Infrastructure
{
    public class Bootstrap : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private StateMachine m_stateMachine;

        [Header("Main Menu")]
        [SerializeField] private GameObject m_mainMenuPanel;

        [Header("Cutscene")]
        [SerializeField] private TimelineManager m_timelineManager;
        [SerializeField] private TimeLineSkip m_timeLineSkip;
        [SerializeField] private GameObject m_cutsceneCanvas;
        [SerializeField][Range(0f, 15f)] private float m_skipDelay = 5f;

        [Header("Dialog")]
        [SerializeField] private GameObject m_dialog;

        private void Awake()
        {
            m_stateMachine.Initialize(
                new MainMenuState(m_mainMenuPanel),
                new CutsceneState(m_stateMachine, m_timelineManager, m_timeLineSkip, m_cutsceneCanvas, m_skipDelay),
                new GameEntryState(),
                new DialogState(m_stateMachine, m_dialog),
                new MiniGameState(),
                new GameExitState());

            m_stateMachine.ChangeState<MainMenuState>();
        }

        /// <summary>
        /// Привязать к кнопке "Играть" в Inspector:
        /// Button.OnClick → Bootstrap.OnPlayButtonClicked
        /// </summary>
        public void OnPlayButtonClicked()
        {
            m_stateMachine.ChangeState<CutsceneState>();
        }
    }
}