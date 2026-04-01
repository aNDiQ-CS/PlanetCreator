using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure.States
{
    public class CutsceneState : MonoBehaviour, IState
    {
        private StateMachine m_stateMachine;
        private TimelineManager m_timelineManager;
        private GameObject m_cutsceneCanvas;

        private void OnEnable()
        {
            m_timelineManager.CutsceneEnded += Exit;
        }

        private void OnDisable()
        {
            m_timelineManager.CutsceneEnded -= Exit;
        }

        public CutsceneState(StateMachine stateMachine, TimelineManager timelineManager, GameObject cutsceneCanvas)
        {
            m_stateMachine = stateMachine;
            m_timelineManager = timelineManager;
            m_cutsceneCanvas = cutsceneCanvas;
        }
        public void Enter()
        {
            m_cutsceneCanvas?.SetActive(true);            
            m_timelineManager?.gameObject.SetActive(true);
            m_timelineManager?.PlayTimeline();            
        }

        public void Exit()
        {
            m_cutsceneCanvas?.SetActive(false);
            m_timelineManager?.gameObject.SetActive(false);
            m_stateMachine.ChangeState<DialogState>();
        }        
    }
}

