using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.Core.Domain
{
    public class Partner : BaseEntity
    {

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Invite> Invites { get; set; }

    }
}
