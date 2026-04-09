using Infrastructure.States;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure
{
    public class StateMachine : MonoBehaviour
    {
        private IState m_currentState;
        private Dictionary<Type, IState> m_states = new();

        /// <summary>
        /// Тип текущего активного стейта (для внешней проверки).
        /// </summary>
        public Type CurrentStateType => m_currentState?.GetType();

        public void Initialize(params IState[] states)
        {
            foreach (var state in states)
            {
                m_states.Add(state.GetType(), state);
            }
        }

        public void ChangeState<T>() where T : IState
        {
            m_currentState?.Exit();
            m_currentState = m_states[typeof(T)];
            m_currentState.Enter();
        }

        /// <summary>
        /// Получить стейт по типу (для вызова специфичных методов, например StepBack).
        /// </summary>
        public T GetState<T>() where T : class, IState
        {
            if (m_states.TryGetValue(typeof(T), out IState state))
                return state as T;
            return null;
        }
    }
}
