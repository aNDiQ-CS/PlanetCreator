using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropButtonActivation : MonoBehaviour
{
    [SerializeField] private DropAndAnimateMiniGame m_miniGame;
    [SerializeField] private Transform m_manipulator;
    [SerializeField] private Button m_button;
    [SerializeField] private Transform m_targetPos;

    [SerializeField][Range(0f, 15f)] private float m_dropDuration = 2f;
    [SerializeField][Range(0f, 15f)] private float m_delay = 5f;

    public void RiseButton()
    {
        m_button.enabled = false;

        StartCoroutine(Delay());
    }

    private IEnumerator Delay()
    {
        if (m_manipulator != null && m_targetPos != null)
        {
            Vector3 startPos = m_manipulator.position;
            Vector3 endPos = m_targetPos.position;
            float elapsed = 0f;

            while (elapsed < m_dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.InOut(Mathf.Clamp01(elapsed / m_dropDuration));
                m_manipulator.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            m_manipulator.position = endPos;
        }

        yield return new WaitForSeconds(m_delay);
        m_miniGame.CompleteGame();
    }
}
