using System.Collections;
using UnityEngine;

namespace Infrastructure.States
{
    public class LevelFlowState : IState
    {
        private readonly StateMachine m_stateMachine;
        private readonly LevelSequence m_sequence;

        private int m_currentStepIndex;
        private Dialogue m_activeDialogue;
        private IMiniGame m_activeMiniGame;
        private Coroutine m_animationCoroutine;

        public LevelFlowState(StateMachine stateMachine, LevelSequence sequence)
        {
            m_stateMachine = stateMachine;
            m_sequence = sequence;
        }

        public void Enter()
        {
            m_currentStepIndex = 0;
            ExecuteCurrentStep();
        }

        public void Exit()
        {
            CleanUpCurrentStep();
        }

        private void ExecuteCurrentStep()
        {
            if (m_sequence == null || m_currentStepIndex >= m_sequence.StepCount)
            {
                m_stateMachine.ChangeState<GameExitState>();
                return;
            }

            var step = m_sequence.GetStep(m_currentStepIndex);
            if (step == null)
            {
                AdvanceToNextStep();
                return;
            }

            switch (step.type)
            {
                case LevelStepType.Dialog:
                    ExecuteDialogStep(step);
                    break;
                case LevelStepType.MiniGame:
                    ExecuteMiniGameStep(step);
                    break;
                case LevelStepType.Animation:
                    ExecuteAnimationStep(step);
                    break;
            }
        }

        // ─────────────────── Dialog ───────────────────

        private void ExecuteDialogStep(LevelStep step)
        {            
            if (step.dialogObject == null)
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): dialogObject не назначен");
                AdvanceToNextStep();
                return;
            }

            m_activeDialogue = step.dialogObject.GetComponent<Dialogue>();
            Debug.LogWarning(m_activeDialogue);

            if (m_activeDialogue != null)
                m_activeDialogue.DialogueFinished += OnDialogueFinished;

            step.dialogObject.SetActive(true);
        }

        private void OnDialogueFinished()
        {
            var step = m_sequence.GetStep(m_currentStepIndex);

            if (m_activeDialogue != null)
                m_activeDialogue.DialogueFinished -= OnDialogueFinished;

            if (step?.dialogObject != null)
                step.dialogObject.SetActive(false);

            m_activeDialogue = null;
            AdvanceToNextStep();
        }

        // ─────────────────── MiniGame ───────────────────

        private void ExecuteMiniGameStep(LevelStep step)
        {
            if (step.miniGameObject == null)
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): miniGameObject не назначен");
                AdvanceToNextStep();
                return;
            }

            step.miniGameObject.SetActive(true);

            m_activeMiniGame = step.miniGameObject.GetComponent<IMiniGame>();

            if (m_activeMiniGame != null)
            {
                m_activeMiniGame.MiniGameCompleted += OnMiniGameCompleted;
                m_activeMiniGame.StartGame();
            }
            else
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): IMiniGame не найден");
                AdvanceToNextStep();
            }
        }

        private void OnMiniGameCompleted()
        {
            var step = m_sequence.GetStep(m_currentStepIndex);

            if (m_activeMiniGame != null)
                m_activeMiniGame.MiniGameCompleted -= OnMiniGameCompleted;

            if (step?.miniGameObject != null)
                step.miniGameObject.SetActive(false);

            m_activeMiniGame = null;
            AdvanceToNextStep();
        }

        // ─────────────────── Animation ───────────────────

        private void ExecuteAnimationStep(LevelStep step)
        {
            if (step.characterAnimator == null)
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): characterAnimator не назначен");
                AdvanceToNextStep();
                return;
            }

            if (!string.IsNullOrEmpty(step.animationTrigger))
                step.characterAnimator.SetTrigger(step.animationTrigger);

            m_animationCoroutine = m_stateMachine.StartCoroutine(WaitForAnimation(step));
        }

        private IEnumerator WaitForAnimation(LevelStep step)
        {
            yield return new WaitForSeconds(step.animationDuration);

            if (step.characterAnimator != null && !string.IsNullOrEmpty(step.idleBoolParam))
                step.characterAnimator.SetBool(step.idleBoolParam, true);

            m_animationCoroutine = null;
            AdvanceToNextStep();
        }

        // ─────────────────── Flow control ───────────────────

        private void AdvanceToNextStep()
        {
            m_currentStepIndex++;
            ExecuteCurrentStep();
        }

        private void CleanUpCurrentStep()
        {
            if (m_activeDialogue != null)
            {
                m_activeDialogue.DialogueFinished -= OnDialogueFinished;
                m_activeDialogue = null;
            }

            if (m_activeMiniGame != null)
            {
                m_activeMiniGame.MiniGameCompleted -= OnMiniGameCompleted;
                m_activeMiniGame.StopGame();
                m_activeMiniGame = null;
            }

            if (m_animationCoroutine != null)
            {
                m_stateMachine.StopCoroutine(m_animationCoroutine);
                m_animationCoroutine = null;
            }

            if (m_sequence != null && m_currentStepIndex < m_sequence.StepCount)
            {
                var step = m_sequence.GetStep(m_currentStepIndex);
                if (step != null)
                {
                    if (step.dialogObject != null) step.dialogObject.SetActive(false);
                    if (step.miniGameObject != null) step.miniGameObject.SetActive(false);
                }
            }
        }
    }
}