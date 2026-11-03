namespace PartnerService.WebHost.Messaging
{
    /// <summary>
    /// Сообщение о выданном приглашении — публикуется PartnerService в очередь RabbitMQ.
    /// UserService слушает эту очередь и асинхронно рассылает приглашение всем клиентам
    /// с указанной деятельностью (создаёт им запись UserInvite).
    /// </summary>
    public sealed class InviteBroadcastMessage
    {
        public Guid PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string Activity { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Вместимость коворкинга: UserService не создаст приглашений больше, чем свободных мест.
        /// </summary>
        public int Capacity { get; set; }
    }
}
