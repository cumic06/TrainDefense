using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cumic.Events
{
    public static class GameEventSystem
    {
        private readonly static Dictionary<Type, List<Delegate>> _events = new();
        private readonly static Dictionary<Type, List<Delegate>> _queries = new();

        public static void Subscribe<T>(Action<T> onEvent)
        {
            Type key = typeof(T);

            if (!_events.ContainsKey(key))
            {
                _events.Add(key, new List<Delegate> { onEvent });
                return;
            }

            if (_events.ContainsKey(key))
            {
                _events[key].Add(onEvent);
            }
        }

        public static void Unsubscribe<T>(Action<T> onEvent)
        {
            Type key = typeof(T);

            if (_events.ContainsKey(key))
            {
                _events[key].Remove(onEvent);
            }
        }

        public static void Publish<T>(T eventData)
        {
            Type key = typeof(T);

            if (!_events.TryGetValue(key, out List<Delegate> events))
            {
                Debug.LogWarning($"{typeof(T)} event is Null");
                return;
            }

            // 핸들러 내부에서 같은 이벤트를 구독/해제해도 순회가 깨지지 않도록 스냅샷을 순회하고,
            // 한 핸들러의 예외가 나머지 핸들러(Resume, 터렛 EngageStart 등)를 건너뛰지 않도록 격리한다.
            Delegate[] snapshot = events.ToArray();

            foreach (var eventAction in snapshot)
            {
                try
                {
                    Action<T> action = eventAction as Action<T>;
                    action?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Subscribe<TRequest, TResponse>(Func<TRequest, TResponse> onQuery)
        {
            Type key = typeof(TRequest);

            if (!_queries.ContainsKey(key))
            {
                _queries.Add(key, new List<Delegate> { onQuery });
                return;
            }

            if (_queries.ContainsKey(key))
            {
                _queries[key].Add(onQuery);
            }
        }

        public static void Unsubscribe<TRequest, TResponse>(Func<TRequest, TResponse> onQuery)
        {
            Type key = typeof(TRequest);

            if (_queries.ContainsKey(key))
            {
                _queries[key].Remove(onQuery);
            }
        }

        public static TResponse Query<TRequest, TResponse>(TRequest requestData)
        {
            Type key = typeof(TRequest);

            if (!_queries.ContainsKey(key))
            {
                Debug.LogWarning($"{typeof(TRequest)} query handler is not registered");
                return default;
            }

            List<Delegate> queryHandlers = _queries[key];

            if (queryHandlers.Count > 0)
            {
                Func<TRequest, TResponse> handler = queryHandlers[0] as Func<TRequest, TResponse>;
                return handler != null ? handler.Invoke(requestData) : default;
            }

            return default;
        }
    }
}