using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateCameraMain : MonoBehaviour
{
    [SerializeField] private Animator m_cameraAnim;
    public void StateFalse()
    {
        m_cameraAnim.enabled = false;
    }
}
