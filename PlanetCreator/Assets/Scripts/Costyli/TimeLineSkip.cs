using Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeLineSkip : MonoBehaviour
{
    [SerializeField] private StateMachine m_stateMachine;

    public event Action CutsceneSkipped;
    
    public void SkipCutscene()
    {
        CutsceneSkipped?.Invoke();
    }
}
