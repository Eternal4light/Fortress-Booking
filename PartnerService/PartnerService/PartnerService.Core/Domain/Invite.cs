using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.Core.Domain
{
    public class Invite
    {
        public string Code { get; set; }

        public string ServiceInfo { get; set; }

        public Guid PartnerId { get; set; }

        public virtual Partner Partner { get; set; }

        public virtual Activity Preference { get; set; }

        public Guid ActivityId { get; set; }
    }
}
