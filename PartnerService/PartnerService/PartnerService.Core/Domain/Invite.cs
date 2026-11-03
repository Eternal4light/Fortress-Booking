using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.Core.Domain
{
    public class Invite : BaseEntity
    {
        public string? Code { get; set; }

        public string? ServiceInfo { get; set; }

        /// <summary>
        /// Текст приглашения, переданный партнёром/менеджером при выдаче.
        /// </summary>
        public string? Description { get; set; }

        public Guid PartnerId { get; set; }

        public virtual Partner? Partner { get; set; }

        public Guid ActivityId { get; set; }

        public virtual Activity? Activity { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    }
}
