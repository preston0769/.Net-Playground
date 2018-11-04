using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Interface
{
    public interface IEventHandler<in TIntegrationEvent> : IEventHandler where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);

    }

    public interface IDynamicEventHandler
    {
        Task Handle(dynamic eventData);
    }


    public interface IEventHandler
    {
    }
}
