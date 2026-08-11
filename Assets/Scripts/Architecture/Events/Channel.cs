using HalloGames.Extensions.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Events
{
    public partial class EventBus
    {
        private class Channel<TEvent> : IDisposable
            where TEvent : struct, IEvent
        {
            private List<Action<TEvent>> _actions = new();

            public void Dispose()
            {
                _actions.Clear();
            }

            public void Publish(TEvent @event)
            {
                var needToClear = false;

                for (int i = 0; i < _actions.Count; i++)
                {
                    var @action = _actions[i];
                    if (action == null)
                    {
                        needToClear = true;
                        continue;
                    }

                    try
                    {
                        action.Invoke(@event);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                if (needToClear)
                    _actions.Compact();
            }

            public IDisposable Subscribe(Action<TEvent> action)
            {
                if (_actions.Contains(action))
                {
                    Debug.LogError($"Duplicate subscription to {typeof(TEvent).Name}");
                    return EmptySubscription.Instance;
                }

                _actions.Add(action);
                return new Subscription<TEvent>(this, action);
            }

            public void Unsubscribe(Action<TEvent> action)
            {
                if (_actions.Count == 0)
                    return;

                var id = _actions.IndexOf(action);
                if (id < 0)
                    return;

                _actions[id] = null;
            }
        }
    }
}
