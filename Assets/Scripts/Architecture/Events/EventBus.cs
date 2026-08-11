using System;
using System.Collections.Generic;

namespace HalloGames.Architecture.Events
{
    public interface IEventBus
    {
        public void Publish<TEvent>(TEvent @event) where TEvent : struct, IEvent;
        public IDisposable Subscribe<TEvent>(Action<TEvent> action) where TEvent : struct, IEvent;
        public void Unsubscribe<TEvent>(Action<TEvent> action) where TEvent : struct, IEvent;
    }

    public interface IEvent
    {
    
    }

    public partial class EventBus : IEventBus, IDisposable
    {
        private Dictionary<Type, object> _channels = new();

        private Channel<TEvent> GetChannel<TEvent>() where TEvent : struct, IEvent
        {
            var type = typeof(TEvent);
            if (!_channels.TryGetValue(type, out var channel))
            {
                channel = new Channel<TEvent>(); 
                _channels.Add(type, channel);
            }

            return (Channel<TEvent>)channel;
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : struct, IEvent =>
            GetChannel<TEvent>().Publish(@event);

        public IDisposable Subscribe<TEvent>(Action<TEvent> action) where TEvent : struct, IEvent =>
            GetChannel<TEvent>().Subscribe(action);

        public void Unsubscribe<TEvent>(Action<TEvent> action) where TEvent : struct, IEvent =>
            GetChannel<TEvent>().Unsubscribe(action);

        public void Dispose()
        {
            _channels.Clear();
        }
    }
}
