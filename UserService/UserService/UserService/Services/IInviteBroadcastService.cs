using UserService.Dtos;

namespace UserService.Services
{
    /// <summary>
    /// Рассылает приглашение всем клиентам с указанной деятельностью — создаёт каждому
    /// запись UserInvite. Используется и обработчиком очереди RabbitMQ (основной путь —
    /// приглашение от PartnerService), и HTTP-эндпоинтом api/users/invites/broadcast
    /// (оставлен как ручной/тестовый вызов с тем же служебным ключом).
    /// </summary>
    public interface IInviteBroadcastService
    {
        Task<int> BroadcastAsync(BroadcastInviteRequest request, CancellationToken ct);
    }
}
