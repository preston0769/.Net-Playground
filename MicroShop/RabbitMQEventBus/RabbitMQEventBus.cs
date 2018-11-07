using Autofac;
using Microshop.Infrastructure.EventBus;
using Microshop.Infrastructure.EventBus.Interface;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Microshop.Infrastructure.RabbitMQEventBus
{
    public class RabbitMQEventBus : IEventBus, IDisposable
    {
        const string BROKER_NAME = "microshop_event_bus";

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger<RabbitMQEventBus> _logger;
        private readonly IEventBusSubscriptionManager _subManager;
        private readonly ILifetimeScope _autofac;

        private readonly string AUTOFAC_SCOPE_NAME = "microshop_event_bus";
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private string _queueName;

        public RabbitMQEventBus(IRabbitMQPersistentConnection persistentConnection, ILogger<RabbitMQEventBus> logger,
              ILifetimeScope autofac, IEventBusSubscriptionManager subsManager, string queueName = null, int retry = 1)
        {
            _persistentConnection = persistentConnection;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subManager = subsManager ?? new InMemoryEventSubscriptionManager();
            _queueName = queueName;
            _consumerChannel = CreateConsumerChannel();
            _autofac = autofac;
            _retryCount = retry;
            _subManager.onEventRemoved += subsManager_OnEventRemoved;

        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");
            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea)=>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);

                await ProcessEvent(eventName, message);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            channel.CallbackException += (sender, ea)=>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
            };
            return channel;
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_subManager.HasSubscriptionForEvent(eventName))
            {
                using(var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                {
                    var subscriptions = _subManager.GetSubscriptionsForEvent(eventName);
                    foreach(var subscription in subscriptions)
                    {
                        if (subscription.IsDynamic)
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType) as IDynamicEventHandler;
                            dynamic eventData = JObject.Parse(message);
                            await handler.Handle(eventData);
                        }
                        else
                        {
                            var eventType = _subManager.GetEventType(eventName);
                            var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            var concreteHandler = typeof(IEventHandler<>).MakeGenericType(eventType);
                            await (Task) concreteHandler.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }


        private void subsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName, exchange: BROKER_NAME, routingKey: eventName);
                if (_subManager.IsEmpty)
                {
                    _queueName = string.Empty;
                    _consumerChannel.Close();
                }
            }
        }

        public void Dispose()
        {
            if(_consumerChannel != null)
            {
                _consumerChannel.Close();
            }

            _subManager.Clear();
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();

            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                 {
                     _logger.LogWarning(ex.ToString());

                 });

            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType().Name;

                channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent;

                    channel.BasicPublish(exchange: BROKER_NAME,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);

                });

            }
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {

            var eventName = _subManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subManager.AddSubscription<T, TH>();
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicEventHandler
        {
            DoInternalSubscription(eventName);
            _subManager.AddDynamicSubscription<TH>(eventName);
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subManager.HasSubscriptionForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                    _persistentConnection.TryConnect();
                using(var chanel = _persistentConnection.CreateModel())
                {
                    chanel.QueueBind(queue: _queueName, exchange: BROKER_NAME, routingKey: eventName);
                }


            }

        }

        public void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IEventHandler
        {
            _subManager.RemoveSubscription<T, TH>();
        }

        public void UnSubscribeDynamic<TH>(string eventName) where TH : IDynamicEventHandler
        {
            _subManager.RemoveSubscription<TH>(eventName);
        }
    }
}

