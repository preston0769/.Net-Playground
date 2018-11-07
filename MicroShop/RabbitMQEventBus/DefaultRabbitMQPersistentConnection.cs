using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Microshop.Infrastructure.RabbitMQEventBus
{
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {

        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
        private readonly int _retryCount;
        IConnection _connection;
        bool _disposed;

        //Should we use static here ? or singleton will prevent this.
        object sync_root = new object();


        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, ILogger<DefaultRabbitMQPersistentConnection> logger, int retryCount = 3)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _retryCount = retryCount;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action.");

            }
            return _connection.CreateModel();

        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");
            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttemp => TimeSpan.FromSeconds(Math.Pow(2, retryAttemp)), (ex, time) =>
                     {
                         _logger.LogWarning(ex.ToString());
                     });

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();

                });
                if (IsConnected)
                {
                    _connection.ConnectionShutdown += _connection_ConnectionShutdown;
                    _connection.CallbackException += _connection_CallbackException;
                    _connection.ConnectionBlocked += _connection_ConnectionBlocked;
                    _logger.LogInformation($"RabbitMQ persistent connecton acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure event.");
                    return true;
                }
                else
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }

            }
        }

        private void _connection_ConnectionBlocked(object sender, RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown, trying to re-connect...");

            TryConnect();
        }

        private void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection on shutdown, trying to re-connect...");

            TryConnect();
        }

        private void _connection_CallbackException(object sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception, trying to re-connect...");

            TryConnect();
        }
    }
}
