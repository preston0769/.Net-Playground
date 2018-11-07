using Microshop.Infrastructure.EventBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microshop.Infrastructure.EventBus.Interface
{
    public interface IEventBusSubscriptionManager
    {
        event EventHandler<string> onEventRemoved;
        bool IsEmpty { get; }
        void Clear();
        void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IEventHandler;
        void AddDynamicSubscription<TH>(string eventName) where TH : IDynamicEventHandler;

        void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IEventHandler;
        void RemoveSubscription<TH>(string eventName) where TH : IDynamicEventHandler;

        Type GetEventType(string eventName);

        bool HasSubscriptionForEvent(string eventName);

        IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent<T>() where T:IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetSubscriptionsForEvent(string eventName);

        string GetEventKey<T>() where T : IntegrationEvent;

         
        

    }
}
