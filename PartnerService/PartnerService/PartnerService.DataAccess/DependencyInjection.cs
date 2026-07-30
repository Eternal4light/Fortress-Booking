using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.DataAccess.Data;
using PartnerService.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace PartnerService.DataAccess
{
    public static class DependencyInjection
    {

        public static void AddDataAccess(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IRepository<Partner>), (x) =>
                new InMemoryRepository<Partner>(FakeData.Partners));
        }

        public static void AddEfDataAccess(this IServiceCollection services)
        {
       //     services.AddDbContext<DataContext>(builder =>
          //          builder.UseSqlite("Filename=PartnerService.sqlite"));

            services.AddScoped<IRepository<Partner>, EfRepository<Partner>>();

        }
    }
}
