namespace UserService.Messaging
{
    /// <summary>
    /// Настройки подключения к RabbitMQ. Заполняется из секции конфигурации "RabbitMq"
    /// (appsettings.json — localhost для локального запуска, appsettings.Docker.json —
    /// имя сервиса "rabbitmq" в docker-compose).
    /// </summary>
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string InviteBroadcastQueue { get; set; } = "invites.broadcast";
    }
}
