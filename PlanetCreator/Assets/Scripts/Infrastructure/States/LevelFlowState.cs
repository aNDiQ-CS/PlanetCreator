using System.Collections;
using System.Collections.Generic;
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

        /// <summary>
        /// true когда выполняется Cutscene, CameraMove или Animation.
        /// </summary>
        private bool m_isNonInteractiveStep;

        /// <summary>
        /// Полный снимок состояния мира перед каждой мини-игрой.
        /// Ключ — индекс шага мини-игры.
        /// </summary>
        private Dictionary<int, WorldSnapshot> m_miniGameSnapshots = new();

        /// <summary>
        /// Стек индексов мини-игр в порядке прохождения — для быстрого нахождения предыдущей.
        /// </summary>
        private List<int> m_miniGameHistory = new();

        public int CurrentStepIndex => m_currentStepIndex;
        public int TotalSteps => m_sequence != null ? m_sequence.StepCount : 0;

        // ─────────────────── Snapshot ───────────────────

        private struct WorldSnapshot
        {
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public List<AnimatorSnapshot> animators;
        }

        private struct AnimatorSnapshot
        {
            public Animator animator;
            public Vector3 position;
            public Quaternion rotation;
            public string idleBoolParam;
        }

        // ─────────────────── Constructor ───────────────────

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
            m_isNonInteractiveStep = false;
            m_miniGameSnapshots.Clear();
            m_miniGameHistory.Clear();
            ExecuteCurrentStep();
        }

        public void Exit()
        {
            CleanUpCurrentStep();
        }

        // ─────────────────── Step Back (к предыдущей мини-игре) ───────────────────

        /// <summary>
        /// Возвращает к последней пройденной мини-игре.
        /// Перескакивает через Dialog, Animation, CameraMove, Cutscene шаги.
        /// Восстанавливает камеру и все аниматоры к состоянию перед той мини-игрой.
        /// </summary>
        public bool StepBack()
        {
            if (!CanStepBack())
                return false;

            // Очищаем текущий шаг
            CleanUpCurrentStep();
            m_isNonInteractiveStep = false;

            // Откатываем данные текущего шага если это мини-игра
            UndoStepData(m_currentStepIndex);

            // Находим предыдущую мини-игру
            int previousMiniGameStep = FindPreviousMiniGameStep();
            if (previousMiniGameStep < 0)
                return false;

            // Откатываем данные всех мини-игр между текущей позицией и целевой
            for (int i = m_currentStepIndex; i >= previousMiniGameStep; i--)
                UndoStepData(i);

            // Убираем из истории
            if (m_miniGameHistory.Count > 0)
                m_miniGameHistory.RemoveAt(m_miniGameHistory.Count - 1);

            // Восстанавливаем состояние мира
            if (m_miniGameSnapshots.TryGetValue(previousMiniGameStep, out WorldSnapshot snapshot))
                RestoreWorldSnapshot(snapshot);

            m_currentStepIndex = previousMiniGameStep;

            Debug.Log($"[LevelFlow] Возврат к мини-игре на шаге {m_currentStepIndex}");

            ExecuteCurrentStep();
            return true;
        }

        public bool CanStepBack()
        {
            if (m_isNonInteractiveStep)
                return false;

            // Можно вернуться если есть хотя бы одна пройденная мини-игра
            return FindPreviousMiniGameStep() >= 0;
        }

        /// <summary>
        /// Находит индекс предыдущей мини-игры.
        /// Если мы сейчас на мини-игре — ищет предыдущую до неё.
        /// Если мы между мини-играми — возвращает последнюю пройденную.
        /// </summary>
        private int FindPreviousMiniGameStep()
        {
            // Ищем среди пройденных мини-игр
            if (m_miniGameHistory.Count == 0)
                return -1;

            int lastMiniGame = m_miniGameHistory[m_miniGameHistory.Count - 1];

            // Если мы сейчас на этой мини-игре — нужна предпредыдущая
            if (lastMiniGame == m_currentStepIndex)
            {
                if (m_miniGameHistory.Count < 2)
                    return -1;
                return m_miniGameHistory[m_miniGameHistory.Count - 2];
            }

            // Мы за ней — возвращаемся к ней
            return lastMiniGame;
        }

        private void RestoreWorldSnapshot(WorldSnapshot snapshot)
        {
            // Камера
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = snapshot.cameraPosition;
                cam.transform.rotation = snapshot.cameraRotation;
                Debug.Log($"[LevelFlow] Камера восстановлена");
            }

            // Аниматоры (лиса и др.)
            if (snapshot.animators != null)
            {
                foreach (var aSnap in snapshot.animators)
                {
                    if (aSnap.animator == null) continue;
                    aSnap.animator.transform.position = aSnap.position;
                    aSnap.animator.transform.rotation = aSnap.rotation;

                    if (!string.IsNullOrEmpty(aSnap.idleBoolParam))
                        aSnap.animator.SetBool(aSnap.idleBoolParam, false);

                    Debug.Log($"[LevelFlow] Аниматор '{aSnap.animator.gameObject.name}' восстановлен");
                }
            }
        }

        /// <summary>
        /// Создаёт полный снимок состояния мира: камера + все аниматоры из шагов.
        /// </summary>
        private WorldSnapshot CaptureWorldSnapshot()
        {
            var snapshot = new WorldSnapshot();

            Camera cam = Camera.main;
            if (cam != null)
            {
                snapshot.cameraPosition = cam.transform.position;
                snapshot.cameraRotation = cam.transform.rotation;
            }

            // Собираем все уникальные аниматоры из Animation-шагов
            snapshot.animators = new List<AnimatorSnapshot>();
            HashSet<Animator> seen = new HashSet<Animator>();

            for (int i = 0; i < m_sequence.StepCount; i++)
            {
                var step = m_sequence.GetStep(i);
                if (step == null) continue;

                if (step.type == LevelStepType.Animation && step.characterAnimator != null)
                {
                    if (seen.Add(step.characterAnimator))
                    {
                        snapshot.animators.Add(new AnimatorSnapshot
                        {
                            animator = step.characterAnimator,
                            position = step.characterAnimator.transform.position,
                            rotation = step.characterAnimator.transform.rotation,
                            idleBoolParam = step.idleBoolParam
                        });
                    }
                }
            }

            return snapshot;
        }

        private void UndoStepData(int stepIndex)
        {
            if (m_sequence == null || stepIndex < 0 || stepIndex >= m_sequence.StepCount)
                return;

            var step = m_sequence.GetStep(stepIndex);
            if (step == null || step.type != LevelStepType.MiniGame)
                return;

            if (step.miniGameObject == null)
                return;

            var data = PlanetBuildData.Instance;
            if (data == null) return;

            var miniGame = step.miniGameObject.GetComponent<IMiniGame>();
            if (miniGame == null) return;

            switch (miniGame)
            {
                case SizeMiniGame:
                    data.size = Planets.Size.Small;
                    break;
                case MassMiniGame:
                    data.mass = Planets.Mass.Light;
                    break;
                case RemotenessMiniGame:
                    data.remoteness = Planets.Remoteness.Near;
                    break;
                case SatellitesMiniGame:
                    data.satellites = Planets.SatellitesOrRings.None;
                    break;
                case MigrationMiniGame:
                    data.migration = Planets.Migration.No;
                    break;
                case ChemicalFlaskManager flaskManager:
                    flaskManager.ResetGame();
                    break;
            }
        }

        // ─────────────────── Step execution ───────────────────

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
                    m_isNonInteractiveStep = false;
                    ExecuteDialogStep(step);
                    break;
                case LevelStepType.MiniGame:
                    m_isNonInteractiveStep = false;
                    ExecuteMiniGameStep(step);
                    break;
                case LevelStepType.Animation:
                    m_isNonInteractiveStep = true;
                    ExecuteAnimationStep(step);
                    break;
                case LevelStepType.CameraMove:
                    m_isNonInteractiveStep = true;
                    ExecuteCameraMoveStep(step);
                    break;
                case LevelStepType.Cutscene:
                    m_isNonInteractiveStep = true;
                    ExecuteCutsceneStep(step);
                    break;
            }
        }

        // ─────────────────── Dialog ───────────────────

        private void ExecuteDialogStep(LevelStep step)
        {
            if (step.dialogObject == null)
            {
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
                AdvanceToNextStep();
                return;
            }

            // Сохраняем снимок мира ПЕРЕД запуском мини-игры
            m_miniGameSnapshots[m_currentStepIndex] = CaptureWorldSnapshot();
            m_miniGameHistory.Add(m_currentStepIndex);

            step.miniGameObject.SetActive(true);
            m_activeMiniGame = step.miniGameObject.GetComponent<IMiniGame>();

            if (m_activeMiniGame != null)
            {
                m_activeMiniGame.MiniGameCompleted += OnMiniGameCompleted;
                m_activeMiniGame.StartGame();
            }
            else
            {
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
                m_isNonInteractiveStep = false;
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
            m_isNonInteractiveStep = false;
            AdvanceToNextStep();
        }

        // ─────────────────── Cutscene ───────────────────

        private void ExecuteCutsceneStep(LevelStep step)
        {
            if (step.timelineManager == null)
            {
                m_isNonInteractiveStep = false;
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
            m_isNonInteractiveStep = false;
            AdvanceToNextStep();
        }

        // ─────────────────── CameraMove ───────────────────

        private void ExecuteCameraMoveStep(LevelStep step)
        {
            if (step.cameraTarget == null)
            {
                m_isNonInteractiveStep = false;
                AdvanceToNextStep();
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                m_isNonInteractiveStep = false;
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
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / duration));
                cam.position = Vector3.Lerp(startPos, target.position, t);
                cam.rotation = Quaternion.Slerp(startRot, target.rotation, t);
                yield return null;
            }

            cam.position = target.position;
            cam.rotation = target.rotation;
            m_activeCoroutine = null;
            m_isNonInteractiveStep = false;
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
