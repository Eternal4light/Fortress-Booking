using PartnerService.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.DataAccess.Data
{
    public static class FakeData
    {
        public static List<Partner> Partners => new List<Partner>()
        {
            new Partner()
            {
                Id = Guid.Parse("7d994823-8226-4273-b063-1a95f3cc1df8"),
                Name = "Коворкинг 1",
                IsActive = true
            }
        };
    }
}
