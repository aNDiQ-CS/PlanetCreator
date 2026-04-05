using System;
using UnityEngine;

namespace Infrastructure
{
    public enum LevelStepType
    {
        Dialog,
        MiniGame,
        Animation,
        CameraMove,
        Cutscene
    }

    [Serializable]
    public class LevelStep
    {
        public string label;
        public LevelStepType type;

        [Range(0f, 10f)]
        public float delayBeforeNext = 0f;

        public GameObject dialogObject;
        public GameObject miniGameObject;

        public Animator characterAnimator;
        public string animationTrigger;
        public string idleBoolParam;
        [Range(0f, 30f)]
        public float animationDuration = 2f;

        public Transform cameraTarget;
        [Range(0.1f, 10f)]
        public float cameraMoveDuration = 1.5f;

        public TimelineManager timelineManager;
    }
}