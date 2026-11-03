using Microsoft.EntityFrameworkCore;
using PartnerService.DataAccess.Repositories;
using System.Linq;

namespace PartnerService.DataAccess.Data
{
    public class EfDbInitializer
        : IDbInitializer
    {
        private readonly DataContext _dataContext;

        public EfDbInitializer(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Создаёт схему БД при первом запуске и засевает демо-данные, если таблицы
        /// ещё пустые. Идемпотентно — при повторных запусках (перезапуск контейнера,
        /// docker compose up после down) уже сохранённые коворкинги, деятельности и
        /// приглашения не затираются.
        /// </summary>
        public void InitializeDb()
        {
            _dataContext.Database.EnsureCreated();

            // EnsureCreated не меняет уже существующие таблицы, а миграций в проекте нет.
            // Если база создана до появления вместимости коворкинга, колонки Capacity в
            // "Partners" нет — добавляем её (идемпотентно); уже сохранённым коворкингам
            // достаётся значение по умолчанию 10, поменять его можно прямо в базе.
            _dataContext.Database.ExecuteSqlRaw(
                "ALTER TABLE \"Partners\" ADD COLUMN IF NOT EXISTS \"Capacity\" integer NOT NULL DEFAULT 10");

            if (!_dataContext.Partners.Any())
            {
                _dataContext.Partners.AddRange(FakeData.Partners);
            }

            if (!_dataContext.Activities.Any())
            {
                _dataContext.Activities.AddRange(FakeData.Activities);
            }

            _dataContext.SaveChanges();
        }
    }
}
