using HalloGames.Architecture.Singletones;
using System;

namespace HalloGames.Architecture.Events
{
    public class EventsManager : LazySingletone<EventsManager>
    {
        private readonly IEventBus _eventBus = new EventBus();

        public static void Publish<TEvent>(TEvent @event) where TEvent : struct, IEvent
        {
            Instance._eventBus.Publish(@event);
        }

        public static IDisposable Subscribe<TEvent>(Action<TEvent> action) where TEvent : struct, IEvent
        {
            return Instance._eventBus.Subscribe(action);
        }
    }
}   