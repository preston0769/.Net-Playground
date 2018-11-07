using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microshop.Infrastructure.EventBus.Interface
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);

        void Subscribe<T, TH>() where T : IntegrationEvent where TH : IEventHandler;
        void SubscribeDynamic<TH>(string eventName) where TH : IDynamicEventHandler;
        void UnSubscribeDynamic<TH>(string eventName) where TH : IDynamicEventHandler;
        void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IEventHandler;

    }
}
