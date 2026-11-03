using PartnerService.WebHost.Messaging;

namespace PartnerService.WebHost.Services
{
    /// <summary>
    /// Публикует событие о выданном приглашении в очередь RabbitMQ, чтобы UserService
    /// асинхронно разослал его клиентам с подходящей деятельностью.
    /// </summary>
    public interface IInvitePublisher
    {
        /// <returns>
        /// true — сообщение успешно поставлено в очередь; false — RabbitMQ недоступен
        /// (приглашение при этом уже сохранено в PartnerService, рассылка просто не
        /// произойдёт, пока брокер не восстановится и приглашение не будет выдано заново).
        /// </returns>
        Task<bool> PublishAsync(InviteBroadcastMessage message, CancellationToken ct);
    }
}
