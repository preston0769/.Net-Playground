using EventBus.Interface;
using MicroShop.Infrastructure.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventBus
{
    public class InMemoryEventSubscriptionManager : IEventSubscriptionManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;

        public InMemoryEventSubscriptionManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }
            
        public event EventHandler<string> onEventRemoved;

        public bool IsEmpty =>!_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();

        public void AddSbuscription<TH>(string eventName) where TH : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {
            throw new NotImplementedException();
        }

        public string GetEventKey<T>() where T : IntegrationEvent
        {
            throw new NotImplementedException();
        }

        public Type GetEventType(string eventName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent(string eventName)
        {
            throw new NotImplementedException();
        }

        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {
            throw new NotImplementedException();
        }

        public void RemoveSubscription<TH>(string eventName) where TH : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

    }
}
