namespace UserService.Domain;

public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string SecondName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>
    /// Деятельность/профессия — заполняется у клиентов, которые ищут коворкинг.
    /// По ней подбираются приглашения от партнёров.
    /// </summary>
    public string? Activity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
