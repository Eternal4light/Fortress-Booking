namespace UserService.Data
{
    /// <summary>
    /// Настройки подключения к MongoDB. Заполняется из секции конфигурации "MongoDb"
    /// (appsettings.json — localhost для локального запуска, appsettings.Docker.json —
    /// имя сервиса "mongo" в docker-compose).
    /// </summary>
    public sealed class MongoDbOptions
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "fortress_users";
        public string UsersCollection { get; set; } = "users";
        public string InvitesCollection { get; set; } = "userInvites";
    }
}
