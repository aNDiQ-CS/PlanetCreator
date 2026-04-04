using UnityEngine;

namespace Infrastructure.States
{
    public class DialogState : IState
    {
        private readonly StateMachine m_stateMachine;
        private readonly GameObject m_dialogObject;
        private readonly Dialogue m_dialogue;

        public DialogState(StateMachine stateMachine, GameObject dialogObject)
        {
            m_stateMachine = stateMachine;
            m_dialogObject = dialogObject;

            // Dialogue — MonoBehaviour на том же объекте
            m_dialogue = dialogObject.GetComponent<Dialogue>();
        }

        public void Enter()
        {
            if (m_dialogue != null)
                m_dialogue.DialogueFinished += OnDialogueFinished;

            if (m_dialogObject != null)
                m_dialogObject.SetActive(true);
        }

        public void Exit()
        {
            if (m_dialogue != null)
                m_dialogue.DialogueFinished -= OnDialogueFinished;

            if (m_dialogObject != null)
                m_dialogObject.SetActive(false);
        }

        private void OnDialogueFinished()
        {
            m_stateMachine.ChangeState<MiniGameState>();
        }
    }
}