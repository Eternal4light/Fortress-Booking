using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.Core.Domain
{
    public class Partner
    {
        public string Name { get; set; }

        public int NumberIssuedPromoCodes { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Invite> Invites { get; set; }

    }
}
