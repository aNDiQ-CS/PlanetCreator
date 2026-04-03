using Infrastructure.States;
using UnityEngine;

namespace Infrastructure
{
    public class Bootstrap : MonoBehaviour
    {
        [Header("States")]
        [SerializeField] private StateMachine m_stateMachine;        

        [Header("Cutscene")]
        [SerializeField] private TimelineManager m_timelineManager;
        [SerializeField] private TimeLineSkip m_timeLineSkip;
        [SerializeField] private GameObject m_cutsceneCanvas;

        [Header("Dialog")]
        [SerializeField] private GameObject m_dialog;

        private void Awake()
        {
            //m_stateMachine = new StateMachine();

            m_stateMachine.Initialize(
                new MainMenuState(m_stateMachine),
                new CutsceneState(m_stateMachine, m_timelineManager, m_timeLineSkip, m_cutsceneCanvas),
                new GameEntryState(m_stateMachine),
                new DialogState(m_stateMachine, m_dialog),
                new MiniGameState(m_stateMachine),
                new GameExitState(m_stateMachine));

            m_stateMachine.ChangeState<MainMenuState>();
        }
    }
}
