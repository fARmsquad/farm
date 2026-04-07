using System;
using System.Collections.Generic;

namespace FarmSimVR.Core.GameState
{
    public class GameEventBus
    {
        private readonly Dictionary<Type, Delegate> _subscriptions = new();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);
            _subscriptions.TryGetValue(eventType, out var existingHandler);
            _subscriptions[eventType] = Delegate.Combine(existingHandler, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var existingHandler))
                return;

            var updatedHandler = Delegate.Remove(existingHandler, handler);
            if (updatedHandler == null)
            {
                _subscriptions.Remove(eventType);
                return;
            }

            _subscriptions[eventType] = updatedHandler;
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            var eventType = typeof(TEvent);
            if (!_subscriptions.TryGetValue(eventType, out var existingHandler))
                return;

            ((Action<TEvent>)existingHandler).Invoke(gameEvent);
        }
    }
}
