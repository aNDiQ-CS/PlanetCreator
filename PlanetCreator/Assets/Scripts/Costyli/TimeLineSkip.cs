using System;
using UnityEngine;

public class TimeLineSkip : MonoBehaviour
{
    public event Action CutsceneSkipped;

    /// <summary>
    /// Привязать к кнопке "Skip" в Inspector:
    /// Button.OnClick → TimeLineSkip.SkipCutscene
    /// </summary>
    public void SkipCutscene()
    {
        CutsceneSkipped?.Invoke();
    }
}