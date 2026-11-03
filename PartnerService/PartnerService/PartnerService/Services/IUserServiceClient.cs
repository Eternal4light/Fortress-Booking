namespace PartnerService.WebHost.Services
{
    public interface IUserServiceClient
    {
        Task<int> BroadcastInviteAsync(
            Guid partnerId,
            string partnerName,
            string? specialization,
            string activity,
            string? description,
            CancellationToken ct);

        /// <summary>
        /// Сколько мест коворкинга сейчас занято приглашениями (в ожидании ответа и принятыми).
        /// null — UserService недоступен или не ответил: окончательный лимит всё равно
        /// применяется при рассылке в UserService, здесь это только ранняя проверка.
        /// </summary>
        Task<int?> GetOccupiedSeatsAsync(Guid partnerId, CancellationToken ct);
    }
}
