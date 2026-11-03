using UserService.Domain;

namespace UserService.Services.Keycloak;

public interface IKeycloakClient
{
    /// <summary>
    /// Логин по email/паролю через Keycloak (Direct Access Grant).
    /// Возвращает access-токен Keycloak или null, если email/пароль неверны либо
    /// учётная запись отключена.
    /// </summary>
    Task<string?> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Создаёт нового пользователя в Keycloak с указанной ролью и возвращает его id
    /// (используется как Id профиля в MongoDB).
    /// </summary>
    Task<Guid> CreateUserAsync(string email, string password, string firstName, string lastName, UserRole role, CancellationToken ct = default);

    /// <summary>Меняет роль пользователя (снимает старую realm-роль, назначает новую).</summary>
    Task UpdateRoleAsync(Guid keycloakUserId, UserRole oldRole, UserRole newRole, CancellationToken ct = default);

    /// <summary>Включает/выключает пользователя в Keycloak (блокирует вход).</summary>
    Task SetEnabledAsync(Guid keycloakUserId, bool enabled, CancellationToken ct = default);

    /// <summary>Удаляет пользователя из Keycloak.</summary>
    Task DeleteUserAsync(Guid keycloakUserId, CancellationToken ct = default);
}
