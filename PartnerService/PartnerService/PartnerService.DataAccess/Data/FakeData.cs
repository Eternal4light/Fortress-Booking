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
                Specialization = "Офисный",
                IsActive = true,
                Capacity = 20
            },
            new Partner()
            {
                Id = Guid.Parse("3f6b9e2a-9b7f-4b8b-9a3a-3a6f9c6a2b1a"),
                Name = "Beauty Space",
                Specialization = "Бьюти",
                IsActive = true,
                Capacity = 10
            }
        };

        public static List<Activity> Activities => new List<Activity>()
        {
            new Activity { Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"), Name = "Красота" },
            new Activity { Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"), Name = "IT" },
            new Activity { Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"), Name = "Юриспруденция" },
            new Activity { Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"), Name = "Маркетинг" },
            new Activity { Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"), Name = "Финансы" }
        };
    }
}
