using UserService.Domain;

namespace UserService.Data;

/// <summary>
/// Демо-профили (админ/менеджер/клиент) для MongoDB. Id совпадают с id соответствующих
/// пользователей в Keycloak, заданными в keycloak/realm-export.json (там же заданы и их
/// пароли) — так демо-логины сразу находят свой профиль по Id после аутентификации
/// через Keycloak.
/// </summary>
public static class FakeData
{
    public static List<User> Users => new()
    {
        new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirstName = "Админ",
            SecondName = string.Empty,
            LastName = "Системы",
            Email = "admin@fortress-booking.local",
            Phone = "+70000000001",
            Role = UserRole.Admin,
            IsActive = true
        },
        new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FirstName = "Мария",
            SecondName = string.Empty,
            LastName = "Менеджерова",
            Email = "manager@fortress-booking.local",
            Phone = "+70000000002",
            Role = UserRole.Manager,
            IsActive = true
        },
        new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FirstName = "Иван",
            SecondName = string.Empty,
            LastName = "Клиентов",
            Email = "client@fortress-booking.local",
            Phone = "+70000000003",
            Role = UserRole.Client,
            Activity = "Красота",
            IsActive = true
        }
    };
}
