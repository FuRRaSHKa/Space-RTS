using System;

namespace HalloGames.Architecture.Events
{
    public partial class EventBus
    {
        private class Subscription<TEvent> : IDisposable
        where TEvent : struct, IEvent
        {
            private Channel<TEvent> _channel;
            private readonly Action<TEvent> _action;

            public Subscription(Channel<TEvent> channel, Action<TEvent> action)
            {
                _channel = channel;
                _action = action;
            }

            public void Dispose()
            {
                if (_channel == null)
                    return;

                _channel.Unsubscribe(_action);
                _channel = null;
            }
        }

        private class EmptySubscription : IDisposable
        {
            public static readonly EmptySubscription Instance = new();

            public void Dispose()
            {
            }
        }
    }
}