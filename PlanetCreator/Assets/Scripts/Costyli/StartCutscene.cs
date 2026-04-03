using Infrastructure;
using Infrastructure.States;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartCutscene : MonoBehaviour
{
    [SerializeField] private StateMachine m_stateMachine;

    public void StartCutsceneState()
    {
        m_stateMachine.ChangeState<CutsceneState>();
    }

}
