using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.DataAccess.Data;
using PartnerService.DataAccess.Repositories;

namespace PartnerService.DataAccess
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Старая in-memory реализация — оставлена как есть, сейчас не используется
        /// (Program.cs вызывает AddEfDataAccess). Можно вернуть на неё через
        /// Program.cs, если возникнут проблемы с PostgreSQL.
        /// </summary>
        public static void AddDataAccess(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IRepository<Partner>), (x) =>
                new InMemoryRepository<Partner>(FakeData.Partners));

            services.AddSingleton(typeof(IRepository<Activity>), (x) =>
                new InMemoryRepository<Activity>(FakeData.Activities));

            services.AddSingleton(typeof(IRepository<Invite>), (x) =>
                new InMemoryRepository<Invite>(Array.Empty<Invite>()));
        }

        /// <summary>
        /// Реальная база данных — PostgreSQL через EF Core. Строка подключения берётся
        /// из ConnectionStrings:Postgres (appsettings.json — localhost для локального
        /// запуска, appsettings.Docker.json — имя сервиса "postgres" в docker-compose).
        /// </summary>
        public static void AddEfDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Строка подключения ConnectionStrings:Postgres не настроена");

            services.AddDbContext<DataContext>(builder => builder.UseNpgsql(connectionString));

            services.AddScoped<IRepository<Partner>, EfRepository<Partner>>();
            services.AddScoped<IRepository<Activity>, EfRepository<Activity>>();
            services.AddScoped<IRepository<Invite>, EfRepository<Invite>>();

            services.AddScoped<IDbInitializer, EfDbInitializer>();
        }
    }
}
