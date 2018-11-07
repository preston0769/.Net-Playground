using RabbitMQ.Client;
using System;

namespace Microshop.Infrastructure.RabbitMQEventBus
{
    public interface IRabbitMQPersistentConnection:IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();
    }
}