namespace PartnerService.WebHost.Models
{
    public class InviteResponse
    {
        public Guid Id { get; set; }

        public Guid PartnerId { get; set; }

        public string PartnerName { get; set; } = string.Empty;

        public Guid ActivityId { get; set; }

        public string ActivityName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime IssuedAt { get; set; }

        /// <summary>
        /// Приглашение поставлено в очередь RabbitMQ на рассылку клиентам. Рассылка
        /// происходит асинхронно, поэтому точное число получателей на момент ответа
        /// не известно (его можно увидеть позже у каждого клиента в "Моих приглашениях").
        /// </summary>
        public bool QueuedForDelivery { get; set; }
    }
}
