using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.Core.Domain
{
    public class Partner : BaseEntity
    {

        public string Name { get; set; }

        public string? Specialization { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Вместимость коворкинга — максимальное число клиентов, которым можно выдать
        /// приглашение (занимают место приглашения в ожидании ответа и принятые).
        /// </summary>
        public int Capacity { get; set; }

        public virtual ICollection<Invite> Invites { get; set; }

    }
}
