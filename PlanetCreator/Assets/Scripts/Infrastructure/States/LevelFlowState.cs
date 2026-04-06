using System.Collections;
using UnityEngine;

namespace Infrastructure.States
{
    public class LevelFlowState : IState
    {
        private readonly StateMachine m_stateMachine;
        private readonly LevelSequence m_sequence;
        private readonly AudioSource m_sound;

        private int m_currentStepIndex;
        private Dialogue m_activeDialogue;
        private IMiniGame m_activeMiniGame;
        private TimelineManager m_activeTimeline;
        private Coroutine m_activeCoroutine;

        public LevelFlowState(StateMachine stateMachine, LevelSequence sequence, AudioSource sound)
        {
            m_stateMachine = stateMachine;
            m_sequence = sequence;
            m_sound = sound;
        }

        public void Enter()
        {
            m_sound.Play();
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
                case LevelStepType.CameraMove:
                    ExecuteCameraMoveStep(step);
                    break;
                case LevelStepType.Cutscene:
                    ExecuteCutsceneStep(step);
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

            m_activeCoroutine = m_stateMachine.StartCoroutine(WaitForAnimation(step));
        }

        private IEnumerator WaitForAnimation(LevelStep step)
        {
            yield return new WaitForSeconds(step.animationDuration);

            if (step.characterAnimator != null && !string.IsNullOrEmpty(step.idleBoolParam))
                step.characterAnimator.SetBool(step.idleBoolParam, true);

            m_activeCoroutine = null;
            AdvanceToNextStep();
        }

        // ─────────────────── Cutscene (Timeline) ───────────────────

        private void ExecuteCutsceneStep(LevelStep step)
        {
            if (step.timelineManager == null)
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): timelineManager не назначен");
                AdvanceToNextStep();
                return;
            }

            m_activeTimeline = step.timelineManager;
            m_activeTimeline.CutsceneEnded += OnCutsceneStepEnded;

            m_activeTimeline.gameObject.SetActive(true);
            m_activeTimeline.PlayTimeline();
        }

        private void OnCutsceneStepEnded()
        {
            if (m_activeTimeline != null)
            {
                m_activeTimeline.CutsceneEnded -= OnCutsceneStepEnded;
                m_activeTimeline = null;
            }

            AdvanceToNextStep();
        }

        // ─────────────────── CameraMove ───────────────────

        private void ExecuteCameraMoveStep(LevelStep step)
        {
            if (step.cameraTarget == null)
            {
                Debug.LogWarning($"[LevelFlow] Step {m_currentStepIndex} ({step.label}): cameraTarget не назначен");
                AdvanceToNextStep();
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[LevelFlow] Camera.main не найдена");
                AdvanceToNextStep();
                return;
            }

            m_activeCoroutine = m_stateMachine.StartCoroutine(
                MoveCamera(cam.transform, step.cameraTarget, step.cameraMoveDuration));
        }

        private IEnumerator MoveCamera(Transform cam, Transform target, float duration)
        {
            Vector3 startPos = cam.position;
            Quaternion startRot = cam.rotation;
            Vector3 endPos = target.position;
            Quaternion endRot = target.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));
                cam.position = Vector3.Lerp(startPos, endPos, t);
                cam.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            cam.position = endPos;
            cam.rotation = endRot;

            m_activeCoroutine = null;
            AdvanceToNextStep();
        }

        // ─────────────────── Flow control ───────────────────

        private void AdvanceToNextStep()
        {
            var currentStep = m_sequence.GetStep(m_currentStepIndex);
            float delay = currentStep != null ? currentStep.delayBeforeNext : 0f;

            if (delay > 0f)
                m_activeCoroutine = m_stateMachine.StartCoroutine(AdvanceWithDelay(delay));
            else
                DoAdvance();
        }

        private IEnumerator AdvanceWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            m_activeCoroutine = null;
            DoAdvance();
        }

        private void DoAdvance()
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

            if (m_activeTimeline != null)
            {
                m_activeTimeline.CutsceneEnded -= OnCutsceneStepEnded;
                m_activeTimeline.StopTimeline();
                m_activeTimeline = null;
            }

            if (m_activeCoroutine != null)
            {
                m_stateMachine.StopCoroutine(m_activeCoroutine);
                m_activeCoroutine = null;
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