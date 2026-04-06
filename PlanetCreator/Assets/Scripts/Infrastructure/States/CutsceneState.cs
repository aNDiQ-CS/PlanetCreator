using System.Collections;
using UnityEngine;

namespace Infrastructure.States
{
    public class CutsceneState : IState
    {
        private readonly StateMachine m_stateMachine;
        private readonly TimelineManager m_timelineManager;
        private readonly TimeLineSkip m_timeLineSkip;
        private readonly GameObject m_cutsceneCanvas;
        private readonly Animator m_cameraAnimator;
        private readonly AudioSource m_audioSource;
        private readonly float m_skipDelay;

        private Coroutine m_delayedTransition;

        public CutsceneState(
            StateMachine stateMachine,
            TimelineManager timelineManager,
            TimeLineSkip timeLineSkip,
            GameObject cutsceneCanvas,
            Animator cameraAnimator,
            AudioSource audioSource,
            float skipDelay = 5f)
        {
            m_stateMachine = stateMachine;
            m_timelineManager = timelineManager;
            m_timeLineSkip = timeLineSkip;
            m_cutsceneCanvas = cutsceneCanvas;
            m_skipDelay = skipDelay;
            m_audioSource = audioSource;
        }

        public void Enter()
        {
            m_delayedTransition = null;

            if (m_timelineManager != null)
                m_timelineManager.CutsceneEnded += OnCutsceneEnded;

            if (m_timeLineSkip != null)
                m_timeLineSkip.CutsceneSkipped += OnCutsceneSkipped;

            if (m_cutsceneCanvas != null)
                m_cutsceneCanvas.SetActive(true);

            if (m_timelineManager != null)
            {
                m_timelineManager.gameObject.SetActive(true);
                m_timelineManager.PlayTimeline();
            }
        }

        public void Exit()
        {
            if (m_timelineManager != null)
                m_timelineManager.CutsceneEnded -= OnCutsceneEnded;

            if (m_timeLineSkip != null)
                m_timeLineSkip.CutsceneSkipped -= OnCutsceneSkipped;

            // Отменяем отложенный переход, если Exit вызван раньше
            if (m_delayedTransition != null)
            {
                m_stateMachine.StopCoroutine(m_delayedTransition);
                m_delayedTransition = null;
            }

            if (m_timelineManager != null)
            {
                m_timelineManager.StopTimeline();
                m_timelineManager.gameObject.SetActive(false);
            }            

            if (m_cutsceneCanvas != null)
                m_cutsceneCanvas.SetActive(false);    
            
            if (m_audioSource != null)
                m_audioSource.gameObject.SetActive(false);
        }

        /// <summary>
        /// Катсцена доиграла до конца → переход сразу.
        /// </summary>
        private void OnCutsceneEnded()
        {
            m_stateMachine.ChangeState<LevelFlowState>();
        }

        /// <summary>
        /// Игрок нажал Skip → останавливаем Timeline, ждём skipDelay, переходим.
        /// </summary>
        private void OnCutsceneSkipped()
        {
            // Останавливаем воспроизведение, но канвас пока оставляем
            if (m_timelineManager != null)
                m_timelineManager.StopTimeline();

            // Защита от повторных нажатий
            if (m_delayedTransition != null) return;

            m_delayedTransition = m_stateMachine.StartCoroutine(DelayedTransition());
        }

        private IEnumerator DelayedTransition()
        {
            yield return new WaitForSeconds(m_skipDelay);

            m_delayedTransition = null;
            m_stateMachine.ChangeState<LevelFlowState>();
        }
    }
}