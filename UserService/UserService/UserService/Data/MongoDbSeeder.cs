using UserService.Repositories;

namespace UserService.Data
{
    /// <summary>
    /// Засевает демо-аккаунты (админ/менеджер/клиент) в MongoDB при первом запуске.
    /// Идемпотентно: если демо-администратор уже есть в базе — считает, что база уже
    /// засеяна, и ничего не делает, чтобы не затирать данные, которые появились сами
    /// (регистрации клиентов, новые сотрудники).
    /// </summary>
    public static class MongoDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userRepository = services.GetRequiredService<IUserRepository>();

            var alreadySeeded = await userRepository.GetByEmailAsync("admin@fortress-booking.local");
            if (alreadySeeded is not null)
            {
                return;
            }

            foreach (var user in FakeData.Users)
            {
                await userRepository.AddAsync(user);
            }
        }
    }
}
