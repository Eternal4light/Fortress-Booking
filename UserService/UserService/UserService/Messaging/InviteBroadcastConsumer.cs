using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UserService.Dtos;
using UserService.Services;

namespace UserService.Messaging
{
    /// <summary>
    /// Фоновый обработчик очереди RabbitMQ: слушает "invites.broadcast" (публикует
    /// PartnerService при выдаче приглашения) и рассылает приглашение клиентам с
    /// подходящей деятельностью. Если RabbitMQ недоступен при старте или соединение
    /// обрывается — переподключается с задержкой, не роняя сервис.
    /// </summary>
    public sealed class InviteBroadcastConsumer : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RabbitMqOptions _options;
        private readonly IInviteBroadcastService _broadcastService;
        private readonly ILogger<InviteBroadcastConsumer> _logger;

        public InviteBroadcastConsumer(
            IOptions<RabbitMqOptions> options,
            IInviteBroadcastService broadcastService,
            ILogger<InviteBroadcastConsumer> logger)
        {
            _options = options.Value;
            _broadcastService = broadcastService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeUntilDisconnectedAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Соединение с RabbitMQ недоступно, повторная попытка через 5 секунд");
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: _options.InviteBroadcastQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _logger.LogInformation("Подключились к RabbitMQ, слушаем очередь {Queue}", _options.InviteBroadcastQueue);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<BroadcastInviteRequest>(json, JsonOptions);

                    if (message is not null)
                    {
                        var count = await _broadcastService.BroadcastAsync(message, stoppingToken);
                        _logger.LogInformation(
                            "Разослано приглашение от «{Partner}» {Count} клиентам с деятельностью «{Activity}»",
                            message.PartnerName, count, message.Activity);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки сообщения из очереди {Queue}", _options.InviteBroadcastQueue);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await channel.BasicConsumeAsync(queue: _options.InviteBroadcastQueue, autoAck: false, consumer: consumer);

            // Держим соединение открытым до остановки сервиса или обрыва связи с брокером.
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
    }
}
