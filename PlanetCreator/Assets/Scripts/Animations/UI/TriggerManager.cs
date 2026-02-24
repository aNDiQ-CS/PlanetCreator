using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerManager : MonoBehaviour
{
    [SerializeField] private Animator[] m_buttonAnim;
    [SerializeField] private int m_codAllAnim;

    public void Trigger(int number)
    {
        if(number == m_codAllAnim)
        {
            for(int i = 0; i < m_buttonAnim.Length; i++)
            {
                m_buttonAnim[i].SetTrigger("ButtonTrigger");
            }
        }
        else
        {
            m_buttonAnim[number].SetTrigger("ButtonTrigger");
        }
    }
}
