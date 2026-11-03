using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PartnerService.WebHost.Messaging;
using RabbitMQ.Client;

namespace PartnerService.WebHost.Services
{
    /// <summary>
    /// Публикует <see cref="InviteBroadcastMessage"/> в очередь RabbitMQ. Соединение
    /// создаётся лениво при первой публикации (в docker-compose RabbitMQ может ещё не
    /// быть готов на момент старта PartnerService) и переиспользуется дальше.
    /// Если брокер недоступен — публикация не роняет запрос, приглашение всё равно
    /// остаётся сохранённым локально в PartnerService.
    /// </summary>
    public sealed class RabbitMqInvitePublisher : IInvitePublisher, IDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqInvitePublisher> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqInvitePublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqInvitePublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> PublishAsync(InviteBroadcastMessage message, CancellationToken ct)
        {
            try
            {
                var channel = await GetChannelAsync();

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _options.InviteBroadcastQueue,
                    body: body);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Не удалось опубликовать приглашение в RabbitMQ (очередь {Queue}). Приглашение сохранено локально, но клиенты о нём не узнают, пока брокер не станет доступен",
                    _options.InviteBroadcastQueue);
                return false;
            }
        }

        private async Task<IChannel> GetChannelAsync()
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_channel is { IsOpen: true })
                {
                    return _channel;
                }

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _options.InviteBroadcastQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                return _channel;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public void Dispose()
        {
            try { _channel?.Dispose(); } catch { /* соединение уже закрыто — игнорируем */ }
            try { _connection?.Dispose(); } catch { /* соединение уже закрыто — игнорируем */ }
            _initLock.Dispose();
        }
    }
}
