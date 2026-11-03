namespace UserService.Services.Keycloak;

/// <summary>
/// Настройки подключения к Keycloak. BaseUrl — адрес сервера Keycloak
/// (для Docker — имя сервиса "keycloak", для локального запуска — localhost:8081).
/// AdminUsername/AdminPassword — учётные данные бутстрап-администратора Keycloak
/// (заданы через KEYCLOAK_ADMIN/KEYCLOAK_ADMIN_PASSWORD в docker-compose), используются
/// только для служебных вызовов Admin REST API (создание/удаление пользователей, роли)
/// через встроенного в realm "master" клиента "admin-cli" — так не требуется вручную
/// настраивать отдельный confidential-клиент с service account, что нельзя было бы
/// проверить компиляцией/запуском в этой среде.
/// </summary>
public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8081";

    public string Realm { get; set; } = "fortress-booking";

    public string FrontendClientId { get; set; } = "fortress-frontend";

    public string AdminUsername { get; set; } = "admin";

    public string AdminPassword { get; set; } = "admin";
}
