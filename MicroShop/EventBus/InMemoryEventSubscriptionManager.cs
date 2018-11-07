using Microshop.Infrastructure.EventBus.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microshop.Infrastructure.EventBus
{
    public class InMemoryEventSubscriptionManager : IEventBusSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;

        public InMemoryEventSubscriptionManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }

        public event EventHandler<string> onEventRemoved;

        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();

        public void AddDynamicSubscription<TH>(string eventName) where TH : IDynamicEventHandler
        {
            DoAddSubscription(typeof(TH), eventName, isDynamic: true);
        }

        private void DoAddSubscription(Type handlerType, string eventName, bool isDynamic)
        {
            if (!_handlers.Keys.Contains(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException($"Hanlder type {handlerType.Name} already registered for '{eventName}", nameof(handlerType));

            }

            if (isDynamic)
            {
                _handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
            }
            else
            {
                _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
            }

        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {
            DoAddSubscription(typeof(TH), GetEventKey<T>(), false);
            _eventTypes.Add(typeof(T));

        }

        public string GetEventKey<T>() where T : IntegrationEvent
        {
            return typeof(T).Name;
        }

        public Type GetEventType(string eventName)
        {
            return _eventTypes.SingleOrDefault(t => t.Name == eventName);
        }

        public IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetSubscriptionsForEvent(key);
        }

        public IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent(string eventName)
        {
            return _handlers[eventName];
        }

        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {
            var subToRemove = FindSubscriptionToRemove(GetEventKey<T>(), typeof(TH));
            DoRemoveHandler(GetEventKey<T>(), subToRemove);
        }

        public void RemoveSubscription<TH>(string eventName) where TH : IDynamicEventHandler
        {
            var subToRemove = FindSubscriptionToRemove(eventName, typeof(TH));

            DoRemoveHandler(eventName, subToRemove);

        }


        private void DoRemoveHandler(string eventName, SubscriptionInfo subToRemove)
        {
            
            if(subToRemove !=null)
            {
                _handlers[eventName].Remove(subToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);

                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                        _eventTypes.Remove(eventType);
                }
                RaiseOnEventRemoved(eventName);
            }
            throw new NotImplementedException();
        }

        private void RaiseOnEventRemoved(string eventName)
        {
            if (onEventRemoved != null)
                onEventRemoved(this, eventName);
        }

        private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType) 
        {
            if (!_handlers.Keys.Contains(eventName))
                return null;
            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);

        }

        public bool HasSubscriptionForEvent(string eventName)
        {
            return _handlers.Keys.Contains(eventName);
        }
    }
}
