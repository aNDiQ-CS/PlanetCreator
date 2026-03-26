using Infrastructure.States;
using UnityEngine;

namespace Infrastructure
{
    public class Bootstrap : MonoBehaviour
    {
        private StateMachine m_stateMachine;

        private void Awake()
        {
            m_stateMachine = new StateMachine();

            m_stateMachine.Initialize(
                new MainMenuState(m_stateMachine),
                new GameEntryState(m_stateMachine),
                new DialogState(m_stateMachine),
                new MiniGameState(m_stateMachine),
                new GameExitState(m_stateMachine));

            m_stateMachine.ChangeState<MainMenuState>();
        }
    }
}
